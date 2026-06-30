using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.OperationalChat;

public sealed class OperationalChatService : IOperationalChatService
{
    private static readonly Regex DomainHeaderPattern = new(
        @"^domain\s+\d+\s*[-—–:]?\s*(?<label>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex DayLinePattern = new(
        @"^(?<day>mon|monday|tue|tues|tuesday|wed|wednesday|thu|thurs|thursday|fri|friday|sat|saturday|sun|sunday|lunes|martes|miercoles|jueves|viernes|sabado|domingo)(?:\s+(?<date>\d{4}-\d{2}-\d{2}|\d{1,2}/\d{1,2}/\d{4}))?\s*:\s*(?<content>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex EventNameFieldPattern = new(
        @"event\s+name\s*:\s*(?<name>.+?)(?:[.;]|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex EventNamePattern = new(
        @"^(?:(?:mon|monday|tue|tues|tuesday|wed|wednesday|thu|thurs|thursday|fri|friday|sat|saturday|sun|sunday|lunes|martes|miercoles|jueves|viernes|sabado|domingo)\s*:\s*)?(?<name>.+?)\s+(?:event|evento)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex PeopleRangePattern = new(
        @"(?<min>\d+)\s*[-/]\s*(?<max>\d+)\s*(?:people|personas)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex PreviousNarrativeBallsPattern = new(
        @"previous\s+narrative.*?(?<value>\d+)\s*(?:balls?|bolas?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex BallsPattern = new(
        @"(?<value>\d+)\s*(?:balls?|bolas?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex LoadsPattern = new(
        @"(?<value>\d+)\s*(?:loads?|cargas?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex CategoryBallsPattern = new(
        @"(?<category>farmers|farmer'?s|restaurant|event|catering|retail|market|service)\s*(?<value>\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex IsoDatePattern = new(
        @"(?<value>\d{4}-\d{2}-\d{2})",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly IOperationalDraftService _operationalDraftService;
    private readonly IOperationalIntentClassifier _operationalIntentClassifier;
    private readonly IOperationalPreviewService _operationalPreviewService;
    private readonly IOperationalWeekSliceService _operationalWeekSliceService;
    private readonly IOperationalSimulationService _operationalSimulationService;

    public OperationalChatService(
        IOperationalDraftService operationalDraftService,
        IOperationalIntentClassifier operationalIntentClassifier,
        IOperationalPreviewService operationalPreviewService,
        IOperationalWeekSliceService operationalWeekSliceService,
        IOperationalSimulationService operationalSimulationService)
    {
        _operationalDraftService = operationalDraftService;
        _operationalIntentClassifier = operationalIntentClassifier;
        _operationalPreviewService = operationalPreviewService;
        _operationalWeekSliceService = operationalWeekSliceService;
        _operationalSimulationService = operationalSimulationService;
    }

    public async Task<OperationalChatResponse> SendAsync(
        OperationalChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceText);

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var actorUserId = ResolveActorUserId(request.ActorUserId);
        var targetWeekStartDate = request.TargetWeekStartDate ?? NormalizeWeekStart(request.ReferenceDate);
        var historicalWeeksToUse = request.HistoricalWeeksToUse < 1
            ? 8
            : request.HistoricalWeeksToUse;

        if (TryBuildStructuredWeekSliceRequest(
                request.SourceText,
                correlationId,
                request.ReferenceDate,
                targetWeekStartDate,
                historicalWeeksToUse,
                actorUserId,
                out var weekSliceRequest,
                out var structuredDetectedIntents,
                out var structuredWarnings))
        {
            return await ExecuteStructuredWeekSliceAsync(
                request.SourceText,
                weekSliceRequest,
                structuredDetectedIntents,
                structuredWarnings,
                cancellationToken);
        }

        var segments = SplitSegments(request.SourceText);

        if (segments.Count == 0)
        {
            return BuildClarificationResponse(
                correlationId,
                request.SourceText,
                [],
                [
                    new OperationalValidationWarning
                    {
                        Code = "empty-operational-chat",
                        Message = "Write at least one operational narrative line before sending it to chat.",
                        RequiresHumanReview = true
                    }
                ],
                "I need at least one operational line before I can interpret it.");
        }

        var analyses = new List<SegmentAnalysis>(segments.Count);
        foreach (var segment in segments)
        {
            analyses.Add(await AnalyzeSegmentAsync(
                segment,
                correlationId,
                request.ReferenceDate,
                targetWeekStartDate,
                historicalWeeksToUse,
                actorUserId,
                cancellationToken));
        }

        var detectedIntents = analyses
            .Select(analysis => analysis.DetectedIntent)
            .ToArray();
        var plannedDrafts = analyses
            .SelectMany(analysis => analysis.Plans)
            .ToArray();
        var initialWarnings = MergeWarnings(analyses.SelectMany(analysis => analysis.InitialWarnings));
        var clarificationRequested = analyses.Any(analysis => analysis.RequiresClarification) || plannedDrafts.Length == 0;

        if (clarificationRequested)
        {
            var clarificationWarnings = new List<OperationalValidationWarning>(initialWarnings);
            clarificationWarnings.AddRange(await CollectClarificationAuditWarningsAsync(
                analyses,
                cancellationToken));

            return BuildClarificationResponse(
                correlationId,
                request.SourceText,
                detectedIntents,
                MergeWarnings(clarificationWarnings),
                "I need a clearer operational narrative before I can create safe drafts.");
        }

        var preflightWarnings = new List<OperationalValidationWarning>(initialWarnings);
        foreach (var draftPlan in plannedDrafts)
        {
            try
            {
                var simulation = await draftPlan.SimulateAsync(cancellationToken);
                preflightWarnings.AddRange(simulation.ValidationWarnings);
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                preflightWarnings.Add(new OperationalValidationWarning
                {
                    Code = "chat-preflight-failed",
                    Message = $"The narrative for '{draftPlan.DetectedIntent.Domain}' needs clarification before drafting. {exception.Message}",
                    RequiresHumanReview = true
                });
            }
        }

        var mergedPreflightWarnings = MergeWarnings(preflightWarnings);
        if (HasBlockingWarnings(mergedPreflightWarnings))
        {
            return BuildClarificationResponse(
                correlationId,
                request.SourceText,
                detectedIntents,
                mergedPreflightWarnings,
                "I found blocking validation warnings, so I did not create any drafts.");
        }

        var createdDrafts = new List<OperationalChatCreatedDraft>(plannedDrafts.Length);
        var responseWarnings = new List<OperationalValidationWarning>(mergedPreflightWarnings);

        foreach (var draftPlan in plannedDrafts)
        {
            var createdDraft = await draftPlan.CreateDraftAsync(cancellationToken);
            var preview = await _operationalPreviewService.BuildPreviewAsync(
                createdDraft.Draft.Id,
                cancellationToken);

            createdDrafts.Add(new OperationalChatCreatedDraft
            {
                DraftId = createdDraft.Draft.Id,
                CorrelationId = createdDraft.Draft.CorrelationId,
                DraftType = createdDraft.Draft.DraftType,
                DraftStatus = createdDraft.Draft.Status.ToString(),
                ReviewPath = $"/operational-drafts/{createdDraft.Draft.Id}",
                RiskLevel = preview.RiskLevel,
                HasConflicts = preview.HasConflicts,
                ValidationWarnings = preview.ValidationWarnings
            });

            responseWarnings.AddRange(preview.ValidationWarnings);
        }

        return new OperationalChatResponse
        {
            CorrelationId = correlationId,
            SourceText = request.SourceText.Trim(),
            NarrativeSummary = $"Created {createdDrafts.Count} draft(s) across {detectedIntents.Select(intent => intent.Domain).Distinct(StringComparer.Ordinal).Count()} operational domain(s). Approval still happens only in Operational Draft Review.",
            RequiresClarification = false,
            DetectedIntents = detectedIntents,
            CreatedDrafts = createdDrafts,
            Warnings = MergeWarnings(responseWarnings)
        };
    }

    private async Task<SegmentAnalysis> AnalyzeSegmentAsync(
        string segment,
        Guid correlationId,
        DateOnly referenceDate,
        DateOnly targetWeekStartDate,
        int historicalWeeksToUse,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        var effectiveDate = ResolveSegmentDate(segment, referenceDate, targetWeekStartDate);
        var classifiedIntent = await _operationalIntentClassifier.ClassifyAsync(
            segment,
            effectiveDate,
            targetWeekStartDate,
            cancellationToken);
        var normalizedSegment = NormalizeForMatching(segment);
        var wantsWeeklyClosingPreview = IsWeeklyClosingPreviewSegment(normalizedSegment);
        var isWeeklyClosing = classifiedIntent is WeeklyClosingIntent || wantsWeeklyClosingPreview;
        var isEvent = IsEventSegment(normalizedSegment);
        var isInventoryTransformation = IsInventoryTransformationSegment(normalizedSegment);
        var isProduction = HasExplicitProductionCue(normalizedSegment);
        var isDailyClosing = IsDailyClosingSegment(normalizedSegment, classifiedIntent);

        if (!isWeeklyClosing && !isEvent && !isInventoryTransformation && isProduction && isDailyClosing)
        {
            return BuildClarificationAnalysis(
                "Ambiguous",
                classifiedIntent,
                segment,
                effectiveDate,
                "Split production tasks and daily usage into separate lines so the chat can draft them safely.",
                () => _operationalSimulationService.SimulateAsync(
                    BuildNarrativeRequest(segment, correlationId, effectiveDate, targetWeekStartDate, historicalWeeksToUse, actorUserId),
                    cancellationToken));
        }

        if (isInventoryTransformation)
        {
            return BuildClarificationAnalysis(
                "Inventory Transformation / Reball",
                classifiedIntent,
                segment,
                effectiveDate,
                "Inventory transformations and reball narratives stay review-only in chat v1. Rewrite them as weekly closing, production, daily closing, or event notes.",
                () => _operationalSimulationService.SimulateAsync(
                    BuildNarrativeRequest(segment, correlationId, effectiveDate, targetWeekStartDate, historicalWeeksToUse, actorUserId),
                    cancellationToken));
        }

        if (isWeeklyClosing)
        {
            if (wantsWeeklyClosingPreview && classifiedIntent is not WeeklyClosingIntent)
            {
                var previewRequest = new OperationalWeeklyClosingPreviewRequest
                {
                    CorrelationId = correlationId,
                    ReferenceDate = effectiveDate,
                    WeekStartDate = targetWeekStartDate,
                    HistoricalWeeksToUse = historicalWeeksToUse,
                    ActorUserId = actorUserId,
                    Notes = segment.Trim()
                };

                return BuildDraftAnalysis(
                    "Weekly Closing Preview",
                    classifiedIntent,
                    segment,
                    effectiveDate,
                    "Preview weekly closing from the current operational truth.",
                    [
                        new DraftPlan(
                            CreateDetectedIntent(
                                "Weekly Closing Preview",
                                classifiedIntent,
                                segment,
                                effectiveDate,
                                "Create weekly closing preview draft."),
                            simulateAsync: token => _operationalSimulationService.SimulateWeeklyClosingPreviewAsync(previewRequest, token),
                            createDraftAsync: token => _operationalDraftService.CreateWeeklyClosingPreviewDraftAsync(previewRequest, token))
                    ]);
            }

            var narrativeRequest = BuildNarrativeRequest(
                segment,
                correlationId,
                effectiveDate,
                targetWeekStartDate,
                historicalWeeksToUse,
                actorUserId);

            return BuildDraftAnalysis(
                "Weekly Closing Preview",
                classifiedIntent,
                segment,
                effectiveDate,
                "Create weekly closing correction draft from the operational narrative.",
                [
                    new DraftPlan(
                        CreateDetectedIntent(
                            "Weekly Closing Preview",
                            classifiedIntent,
                            segment,
                            effectiveDate,
                            "Create weekly closing draft."),
                        simulateAsync: token => _operationalSimulationService.SimulateAsync(narrativeRequest, token),
                        createDraftAsync: token => _operationalDraftService.CreateWeeklyCorrectionDraftAsync(narrativeRequest, token))
                ]);
        }

        if (isEvent)
        {
            if (!TryBuildEventRequest(segment, correlationId, effectiveDate, actorUserId, out var eventRequest, out var prompt))
            {
                return BuildClarificationAnalysis(
                    "External Event",
                    classifiedIntent,
                    segment,
                    effectiveDate,
                    prompt,
                    () => _operationalSimulationService.SimulateAsync(
                        BuildNarrativeRequest(segment, correlationId, effectiveDate, targetWeekStartDate, historicalWeeksToUse, actorUserId),
                        cancellationToken));
            }

            return BuildDraftAnalysis(
                "External Event",
                classifiedIntent,
                segment,
                effectiveDate,
                "Create external event draft.",
                [
                    new DraftPlan(
                        CreateDetectedIntent(
                            "External Event",
                            classifiedIntent,
                            segment,
                            effectiveDate,
                            "Create restaurant event draft."),
                        simulateAsync: token => _operationalSimulationService.SimulateRestaurantEventAsync(eventRequest, token),
                        createDraftAsync: token => _operationalDraftService.CreateRestaurantEventDraftAsync(eventRequest, token))
                ]);
        }

        if (isProduction)
        {
            if (!TryBuildProductionRequests(segment, correlationId, effectiveDate, historicalWeeksToUse, actorUserId, out var productionRequests, out var prompt))
            {
                return BuildClarificationAnalysis(
                    "Production / Dough Tasks",
                    classifiedIntent,
                    segment,
                    effectiveDate,
                    prompt,
                    () => _operationalSimulationService.SimulateAsync(
                        BuildNarrativeRequest(segment, correlationId, effectiveDate, targetWeekStartDate, historicalWeeksToUse, actorUserId),
                        cancellationToken));
            }

            var plans = productionRequests
                .Select(request => new DraftPlan(
                    CreateDetectedIntent(
                        "Production / Dough Tasks",
                        classifiedIntent,
                        segment,
                        effectiveDate,
                        $"Create {request.TaskType} draft."),
                    simulateAsync: token => _operationalSimulationService.SimulateDoughTaskAsync(request, token),
                    createDraftAsync: token => _operationalDraftService.CreateDoughTaskDraftAsync(request, token)))
                .ToArray();

            return BuildDraftAnalysis(
                "Production / Dough Tasks",
                classifiedIntent,
                segment,
                effectiveDate,
                plans.Length == 1
                    ? "Create production draft."
                    : $"Create {plans.Length} production drafts from the same operational line.",
                plans);
        }

        if (isDailyClosing)
        {
            if (!TryBuildDailyClosingRequest(segment, correlationId, effectiveDate, historicalWeeksToUse, actorUserId, out var dailyClosingRequest, out var prompt))
            {
                return BuildClarificationAnalysis(
                    "Daily Closing / Usage",
                    classifiedIntent,
                    segment,
                    effectiveDate,
                    prompt,
                    () => _operationalSimulationService.SimulateAsync(
                        BuildNarrativeRequest(segment, correlationId, effectiveDate, targetWeekStartDate, historicalWeeksToUse, actorUserId),
                        cancellationToken));
            }

            return BuildDraftAnalysis(
                "Daily Closing / Usage",
                classifiedIntent,
                segment,
                effectiveDate,
                "Create daily closing draft.",
                [
                    new DraftPlan(
                        CreateDetectedIntent(
                            "Daily Closing / Usage",
                            classifiedIntent,
                            segment,
                            effectiveDate,
                            "Create daily closing draft."),
                        simulateAsync: token => _operationalSimulationService.SimulateDailyClosingAsync(dailyClosingRequest, token),
                        createDraftAsync: token => _operationalDraftService.CreateDailyClosingDraftAsync(dailyClosingRequest, token))
                ]);
        }

        return BuildClarificationAnalysis(
            "Ambiguous",
            classifiedIntent,
            segment,
            effectiveDate,
            "I could not safely map this line to production, daily closing, event, or weekly closing. Please rewrite it with the operational action and quantity.",
            () => _operationalSimulationService.SimulateAsync(
                BuildNarrativeRequest(segment, correlationId, effectiveDate, targetWeekStartDate, historicalWeeksToUse, actorUserId),
                cancellationToken));
    }

    private static SegmentAnalysis BuildDraftAnalysis(
        string domain,
        OperationalIntent classifiedIntent,
        string segment,
        DateOnly effectiveDate,
        string decision,
        IReadOnlyList<DraftPlan> plans)
    {
        return new SegmentAnalysis
        {
            DetectedIntent = CreateDetectedIntent(
                domain,
                classifiedIntent,
                segment,
                effectiveDate,
                decision),
            RequiresClarification = false,
            Plans = plans
        };
    }

    private static SegmentAnalysis BuildClarificationAnalysis(
        string domain,
        OperationalIntent classifiedIntent,
        string segment,
        DateOnly effectiveDate,
        string prompt,
        Func<Task<OperationalSimulationResult>> simulateForAuditAsync)
    {
        return new SegmentAnalysis
        {
            DetectedIntent = CreateDetectedIntent(
                domain,
                classifiedIntent,
                segment,
                effectiveDate,
                "Clarification required.",
                requiresClarification: true,
                clarificationPrompt: prompt),
            RequiresClarification = true,
            InitialWarnings =
            [
                new OperationalValidationWarning
                {
                    Code = "clarification-required",
                    Message = prompt,
                    RequiresHumanReview = true
                }
            ],
            SimulateForAuditAsync = _ => simulateForAuditAsync()
        };
    }

    private static OperationalChatDetectedIntent CreateDetectedIntent(
        string domain,
        OperationalIntent classifiedIntent,
        string segment,
        DateOnly effectiveDate,
        string decision,
        bool requiresClarification = false,
        string clarificationPrompt = "")
    {
        return new OperationalChatDetectedIntent
        {
            Domain = domain,
            IntentKind = classifiedIntent.Kind.ToString(),
            SourceFragment = segment.Trim(),
            Summary = classifiedIntent.NormalizedSummary,
            Decision = decision,
            EffectiveDate = effectiveDate,
            RequiresClarification = requiresClarification,
            ClarificationPrompt = clarificationPrompt
        };
    }

    private static OperationalNarrativeRequest BuildNarrativeRequest(
        string sourceText,
        Guid correlationId,
        DateOnly referenceDate,
        DateOnly targetWeekStartDate,
        int historicalWeeksToUse,
        string actorUserId)
    {
        return new OperationalNarrativeRequest
        {
            CorrelationId = correlationId,
            SourceText = sourceText.Trim(),
            ReferenceDate = referenceDate,
            TargetWeekStartDate = targetWeekStartDate,
            HistoricalWeeksToUse = historicalWeeksToUse,
            ActorUserId = actorUserId
        };
    }

    private async Task<IReadOnlyList<OperationalValidationWarning>> CollectClarificationAuditWarningsAsync(
        IReadOnlyList<SegmentAnalysis> analyses,
        CancellationToken cancellationToken)
    {
        var warnings = new List<OperationalValidationWarning>();

        foreach (var analysis in analyses)
        {
            if (analysis.RequiresClarification)
            {
                if (analysis.SimulateForAuditAsync is null)
                {
                    continue;
                }

                try
                {
                    var simulation = await analysis.SimulateForAuditAsync(cancellationToken);
                    warnings.AddRange(simulation.ValidationWarnings);
                }
                catch (Exception exception) when (IsRecoverable(exception))
                {
                    warnings.Add(new OperationalValidationWarning
                    {
                        Code = "chat-audit-failed",
                        Message = $"The clarification audit could not interpret '{analysis.DetectedIntent.SourceFragment}'. {exception.Message}",
                        RequiresHumanReview = true
                    });
                }

                continue;
            }

            foreach (var draftPlan in analysis.Plans)
            {
                try
                {
                    var simulation = await draftPlan.SimulateAsync(cancellationToken);
                    warnings.AddRange(simulation.ValidationWarnings);
                }
                catch (Exception exception) when (IsRecoverable(exception))
                {
                    warnings.Add(new OperationalValidationWarning
                    {
                        Code = "chat-preflight-failed",
                        Message = $"The narrative for '{draftPlan.DetectedIntent.Domain}' needs clarification before drafting. {exception.Message}",
                        RequiresHumanReview = true
                    });
                }
            }
        }

        return MergeWarnings(warnings);
    }

    private async Task<OperationalChatResponse> ExecuteStructuredWeekSliceAsync(
        string sourceText,
        OperationalWeekSliceRequest weekSliceRequest,
        IReadOnlyList<OperationalChatDetectedIntent> detectedIntents,
        IReadOnlyList<OperationalValidationWarning> structuredWarnings,
        CancellationToken cancellationToken)
    {
        var result = await _operationalWeekSliceService.ExecuteAsync(weekSliceRequest, cancellationToken);
        var allEnvelopes = result.ProductionDrafts
            .Concat(result.DailyClosingDrafts)
            .Concat(result.EventDrafts)
            .Append(result.WeeklyClosingDraft)
            .ToArray();

        var createdDrafts = new List<OperationalChatCreatedDraft>(allEnvelopes.Length);
        var responseWarnings = new List<OperationalValidationWarning>(structuredWarnings);
        responseWarnings.AddRange(result.ValidationWarnings);

        foreach (var createdDraft in allEnvelopes)
        {
            var preview = await _operationalPreviewService.BuildPreviewAsync(
                createdDraft.Draft.Id,
                cancellationToken);

            createdDrafts.Add(new OperationalChatCreatedDraft
            {
                DraftId = createdDraft.Draft.Id,
                CorrelationId = createdDraft.Draft.CorrelationId,
                DraftType = createdDraft.Draft.DraftType,
                DraftStatus = createdDraft.Draft.Status.ToString(),
                ReviewPath = $"/operational-drafts/{createdDraft.Draft.Id}",
                RiskLevel = preview.RiskLevel,
                HasConflicts = preview.HasConflicts,
                ValidationWarnings = preview.ValidationWarnings
            });

            responseWarnings.AddRange(preview.ValidationWarnings);
        }

        return new OperationalChatResponse
        {
            CorrelationId = result.CorrelationId,
            SourceText = sourceText.Trim(),
            NarrativeSummary = $"Created {createdDrafts.Count} draft(s) across {detectedIntents.Select(intent => intent.Domain).Distinct(StringComparer.Ordinal).Count()} operational domain(s). Approval still happens only in Operational Draft Review.",
            RequiresClarification = false,
            DetectedIntents = detectedIntents,
            CreatedDrafts = createdDrafts,
            Warnings = MergeWarnings(responseWarnings)
        };
    }

    private bool TryBuildStructuredWeekSliceRequest(
        string sourceText,
        Guid correlationId,
        DateOnly referenceDate,
        DateOnly targetWeekStartDate,
        int historicalWeeksToUse,
        string actorUserId,
        out OperationalWeekSliceRequest weekSliceRequest,
        out IReadOnlyList<OperationalChatDetectedIntent> detectedIntents,
        out IReadOnlyList<OperationalValidationWarning> warnings)
    {
        var normalizedSourceText = sourceText.Replace("\r", string.Empty, StringComparison.Ordinal);
        var lines = normalizedSourceText
            .Split('\n', StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        var productionDrafts = new List<OperationalDoughTaskDraftRequest>();
        var dailyClosingAccumulators = new Dictionary<DateOnly, DailyClosingAccumulator>();
        var eventAccumulators = new Dictionary<DateOnly, EventAccumulator>();
        var weeklyPreviewNotes = new List<string>();
        var structuredWarnings = new List<OperationalValidationWarning>();
        var structuredDetectedIntents = new List<OperationalChatDetectedIntent>();
        var domainSet = new HashSet<string>(StringComparer.Ordinal);

        var currentDomain = StructuredOperationalDomain.None;
        var currentDate = (DateOnly?)null;
        var explicitDayLineCount = 0;
        var domainHeaderCount = 0;
        var inventoryDomainMentioned = false;

        foreach (var rawLine in lines)
        {
            if (TryResolveStructuredDomain(rawLine, out var resolvedDomain))
            {
                currentDomain = resolvedDomain;
                currentDate = null;
                domainHeaderCount++;

                if (resolvedDomain == StructuredOperationalDomain.InventoryTransformation)
                {
                    inventoryDomainMentioned = true;
                }

                continue;
            }

            if (TryParseDayLine(rawLine, targetWeekStartDate, referenceDate, out var parsedDate, out var content, out var headerLabel))
            {
                explicitDayLineCount++;
                currentDate = parsedDate;

                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                if (!TryConsumeStructuredLine(
                        currentDomain,
                        parsedDate,
                        headerLabel,
                        content,
                        correlationId,
                        historicalWeeksToUse,
                        actorUserId,
                        productionDrafts,
                        dailyClosingAccumulators,
                        eventAccumulators,
                        weeklyPreviewNotes,
                        structuredWarnings,
                        domainSet,
                        referenceDate,
                        targetWeekStartDate))
                {
                    return FailStructuredWeekSlice(out weekSliceRequest, out detectedIntents, out warnings);
                }

                continue;
            }

            var contentLine = rawLine.TrimStart('-', '*', ' ');
            if (string.IsNullOrWhiteSpace(contentLine))
            {
                continue;
            }

            if (currentDomain == StructuredOperationalDomain.InventoryTransformation)
            {
                inventoryDomainMentioned = true;
                if (ShouldIgnoreInventoryTransformationLine(contentLine))
                {
                    if (contentLine.Contains("source-trace", StringComparison.OrdinalIgnoreCase) ||
                        contentLine.Contains("source-date", StringComparison.OrdinalIgnoreCase) ||
                        contentLine.Contains("doughusagetrace", StringComparison.OrdinalIgnoreCase))
                    {
                        structuredWarnings.Add(new OperationalValidationWarning
                        {
                            Code = "missing-dough-usage-trace",
                            Message = "DoughUsageTrace/source-date tracking was not provided in this weekly slice and still needs to be captured later.",
                            RequiresHumanReview = true
                        });
                    }

                    continue;
                }
            }

            if (currentDomain == StructuredOperationalDomain.None &&
                currentDate is null &&
                !IsWeeklyClosingPreviewSegment(NormalizeForMatching(contentLine)))
            {
                continue;
            }

            if (!TryConsumeStructuredLine(
                    currentDomain,
                    currentDate ?? ResolveSegmentDate(contentLine, referenceDate, targetWeekStartDate),
                    currentDate.HasValue ? $"{currentDate.Value:yyyy-MM-dd}:" : string.Empty,
                    contentLine,
                    correlationId,
                    historicalWeeksToUse,
                    actorUserId,
                    productionDrafts,
                    dailyClosingAccumulators,
                    eventAccumulators,
                    weeklyPreviewNotes,
                    structuredWarnings,
                    domainSet,
                    referenceDate,
                    targetWeekStartDate))
            {
                return FailStructuredWeekSlice(out weekSliceRequest, out detectedIntents, out warnings);
            }
        }

        if (domainHeaderCount == 0 && explicitDayLineCount < 3)
        {
            return FailStructuredWeekSlice(out weekSliceRequest, out detectedIntents, out warnings);
        }

        var dailyClosingDrafts = FinalizeStructuredDailyClosingDrafts(
            dailyClosingAccumulators,
            correlationId,
            historicalWeeksToUse,
            actorUserId);
        var eventDrafts = FinalizeStructuredEventDrafts(
            eventAccumulators,
            correlationId,
            actorUserId);

        if (productionDrafts.Count == 0 &&
            dailyClosingDrafts.Count == 0 &&
            eventDrafts.Count == 0 &&
            weeklyPreviewNotes.Count == 0)
        {
            return FailStructuredWeekSlice(out weekSliceRequest, out detectedIntents, out warnings);
        }

        if (domainHeaderCount == 0 &&
            dailyClosingDrafts.Count == 0 &&
            eventDrafts.Count == 0 &&
            weeklyPreviewNotes.Count == 0)
        {
            return FailStructuredWeekSlice(out weekSliceRequest, out detectedIntents, out warnings);
        }

        if (productionDrafts.Count > 0)
        {
            structuredDetectedIntents.Add(CreateStructuredDetectedIntent(
                "Production / Dough Tasks",
                "ProductionIntent",
                "Create production drafts from structured weekly log.",
                targetWeekStartDate));
        }

        if (dailyClosingDrafts.Count > 0)
        {
            structuredDetectedIntents.Add(CreateStructuredDetectedIntent(
                "Daily Closing / Usage",
                "ConsumptionIntent",
                "Create daily closing drafts from structured weekly log.",
                targetWeekStartDate));
        }

        if (eventDrafts.Count > 0)
        {
            structuredDetectedIntents.Add(CreateStructuredDetectedIntent(
                "External Event",
                "ExternalEventIntent",
                "Create restaurant event draft from structured weekly log.",
                targetWeekStartDate));
        }

        if (inventoryDomainMentioned)
        {
            structuredDetectedIntents.Add(CreateStructuredDetectedIntent(
                "Inventory Transformation / Reball",
                "InventoryIntent",
                "No transformation draft was created. Inventory notes stay informational until a concrete reball, discard, or correction is provided.",
                targetWeekStartDate));
        }

        structuredDetectedIntents.Add(CreateStructuredDetectedIntent(
            "Weekly Closing Preview",
            "WeeklyClosingIntent",
            "Create weekly closing preview draft from structured weekly log.",
            targetWeekStartDate));

        weekSliceRequest = new OperationalWeekSliceRequest
        {
            CorrelationId = correlationId,
            WeekStartDate = targetWeekStartDate,
            ReferenceDate = referenceDate,
            HistoricalWeeksToUse = historicalWeeksToUse,
            ActorUserId = actorUserId,
            ProductionDrafts = productionDrafts,
            DailyClosingDrafts = dailyClosingDrafts,
            EventDrafts = eventDrafts,
            WeeklyClosingNotes = weeklyPreviewNotes.Count == 0
                ? $"Weekly closing preview for {targetWeekStartDate:yyyy-MM-dd} through {targetWeekStartDate.AddDays(6):yyyy-MM-dd} structured operational slice."
                : string.Join(' ', weeklyPreviewNotes)
        };
        detectedIntents = structuredDetectedIntents;
        warnings = MergeWarnings(structuredWarnings);
        return true;
    }

    private static bool TryResolveStructuredDomain(string line, out StructuredOperationalDomain domain)
    {
        var match = DomainHeaderPattern.Match(line);
        if (!match.Success)
        {
            domain = StructuredOperationalDomain.None;
            return false;
        }

        var normalizedLabel = NormalizeForMatching(match.Groups["label"].Value);
        domain = normalizedLabel switch
        {
            var label when label.Contains("production", StringComparison.Ordinal) => StructuredOperationalDomain.Production,
            var label when label.Contains("daily closing", StringComparison.Ordinal) || label.Contains("consumption", StringComparison.Ordinal) => StructuredOperationalDomain.DailyClosing,
            var label when label.Contains("external event", StringComparison.Ordinal) => StructuredOperationalDomain.ExternalEvent,
            var label when label.Contains("inventory transformation", StringComparison.Ordinal) || label.Contains("reball", StringComparison.Ordinal) => StructuredOperationalDomain.InventoryTransformation,
            var label when label.Contains("weekly closing", StringComparison.Ordinal) => StructuredOperationalDomain.WeeklyClosingPreview,
            _ => StructuredOperationalDomain.None
        };

        return domain != StructuredOperationalDomain.None;
    }

    private static bool TryParseDayLine(
        string line,
        DateOnly targetWeekStartDate,
        DateOnly referenceDate,
        out DateOnly parsedDate,
        out string content,
        out string headerLabel)
    {
        var match = DayLinePattern.Match(line);
        if (!match.Success)
        {
            parsedDate = default;
            content = string.Empty;
            headerLabel = string.Empty;
            return false;
        }

        var dateToken = match.Groups["date"].Value;
        if (!TryParseSupportedDate(dateToken, out parsedDate))
        {
            parsedDate = ResolveSegmentDate(match.Groups["day"].Value, referenceDate, targetWeekStartDate);
        }

        headerLabel = $"{match.Groups["day"].Value} {parsedDate:yyyy-MM-dd}:";
        content = match.Groups["content"].Value.Trim();
        return true;
    }

    private bool TryConsumeStructuredLine(
        StructuredOperationalDomain currentDomain,
        DateOnly effectiveDate,
        string headerLabel,
        string content,
        Guid correlationId,
        int historicalWeeksToUse,
        string actorUserId,
        List<OperationalDoughTaskDraftRequest> productionDrafts,
        Dictionary<DateOnly, DailyClosingAccumulator> dailyClosingAccumulators,
        Dictionary<DateOnly, EventAccumulator> eventAccumulators,
        List<string> weeklyPreviewNotes,
        List<OperationalValidationWarning> warnings,
        HashSet<string> domainSet,
        DateOnly referenceDate,
        DateOnly targetWeekStartDate)
    {
        var normalizedContent = NormalizeForMatching(content);
        var effectiveDomain = currentDomain;

        if (effectiveDomain == StructuredOperationalDomain.None)
        {
            if (IsWeeklyClosingPreviewSegment(normalizedContent))
            {
                effectiveDomain = StructuredOperationalDomain.WeeklyClosingPreview;
            }
            else if (IsEventSegment(normalizedContent))
            {
                effectiveDomain = StructuredOperationalDomain.ExternalEvent;
            }
            else if (HasExplicitProductionCue(normalizedContent))
            {
                effectiveDomain = StructuredOperationalDomain.Production;
            }
            else if (ParseUsageBreakdown(SanitizeUsageContent(content)).Count > 0 || BallsPattern.IsMatch(normalizedContent))
            {
                effectiveDomain = StructuredOperationalDomain.DailyClosing;
            }
        }

        switch (effectiveDomain)
        {
            case StructuredOperationalDomain.Production:
            {
                var segment = string.IsNullOrWhiteSpace(headerLabel)
                    ? content
                    : $"{headerLabel} {content}";
                if (!TryBuildProductionRequests(segment, correlationId, effectiveDate, historicalWeeksToUse, actorUserId, out var requests, out _))
                {
                    return false;
                }

                productionDrafts.AddRange(requests);
                domainSet.Add("Production / Dough Tasks");
                return true;
            }
            case StructuredOperationalDomain.DailyClosing:
            {
                var accumulator = GetOrCreateDailyClosingAccumulator(dailyClosingAccumulators, effectiveDate);
                var sanitizedContent = SanitizeUsageContent(content);
                if (sanitizedContent.Contains("handled separately", StringComparison.OrdinalIgnoreCase) ||
                    sanitizedContent.Contains("domain 3", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (sanitizedContent.StartsWith("total", StringComparison.OrdinalIgnoreCase) ||
                    sanitizedContent.Contains("excluding event", StringComparison.OrdinalIgnoreCase))
                {
                    accumulator.DeclaredTotalBalls = TryParseBalls(sanitizedContent) ?? accumulator.DeclaredTotalBalls;
                    accumulator.Notes.Add(content.Trim());
                    domainSet.Add("Daily Closing / Usage");
                    return true;
                }

                var components = ParseUsageBreakdown(sanitizedContent);
                if (components.Count == 0)
                {
                    return true;
                }

                accumulator.Components.AddRange(components);
                accumulator.Notes.Add(content.Trim());
                domainSet.Add("Daily Closing / Usage");
                return true;
            }
            case StructuredOperationalDomain.ExternalEvent:
            {
                var accumulator = GetOrCreateEventAccumulator(eventAccumulators, effectiveDate);
                accumulator.Notes.Add(content.Trim());

                if (TryExtractStructuredEventName(content, out var eventName))
                {
                    accumulator.Name = eventName;
                }

                var peopleRangeMatch = PeopleRangePattern.Match(content);
                if (peopleRangeMatch.Success &&
                    int.TryParse(peopleRangeMatch.Groups["min"].Value, out var minimumPeople) &&
                    int.TryParse(peopleRangeMatch.Groups["max"].Value, out var maximumPeople))
                {
                    accumulator.ExpectedPeopleMinimum = minimumPeople;
                    accumulator.ExpectedPeopleMaximum = maximumPeople;
                }

                var previousNarrativeMatch = PreviousNarrativeBallsPattern.Match(content);
                if (previousNarrativeMatch.Success &&
                    int.TryParse(previousNarrativeMatch.Groups["value"].Value, out var previousNarrativeBalls))
                {
                    accumulator.PreviousNarrativeDoughBalls = previousNarrativeBalls;
                }

                var doughUsed = TryParseBalls(content);
                if (doughUsed.HasValue &&
                    (!previousNarrativeMatch.Success || doughUsed.Value != accumulator.PreviousNarrativeDoughBalls))
                {
                    accumulator.EstimatedDoughBalls = doughUsed.Value;
                }

                domainSet.Add("External Event");
                return true;
            }
            case StructuredOperationalDomain.InventoryTransformation:
                return ShouldIgnoreInventoryTransformationLine(content);
            case StructuredOperationalDomain.WeeklyClosingPreview:
            {
                weeklyPreviewNotes.Add(content.Trim());
                domainSet.Add("Weekly Closing Preview");
                return true;
            }
            default:
                return false;
        }
    }

    private static DailyClosingAccumulator GetOrCreateDailyClosingAccumulator(
        IDictionary<DateOnly, DailyClosingAccumulator> accumulators,
        DateOnly date)
    {
        if (!accumulators.TryGetValue(date, out var accumulator))
        {
            accumulator = new DailyClosingAccumulator(date);
            accumulators[date] = accumulator;
        }

        return accumulator;
    }

    private static EventAccumulator GetOrCreateEventAccumulator(
        IDictionary<DateOnly, EventAccumulator> accumulators,
        DateOnly date)
    {
        if (!accumulators.TryGetValue(date, out var accumulator))
        {
            accumulator = new EventAccumulator(date);
            accumulators[date] = accumulator;
        }

        return accumulator;
    }

    private static IReadOnlyList<OperationalDailyClosingDraftRequest> FinalizeStructuredDailyClosingDrafts(
        IReadOnlyDictionary<DateOnly, DailyClosingAccumulator> accumulators,
        Guid correlationId,
        int historicalWeeksToUse,
        string actorUserId)
    {
        return accumulators
            .OrderBy(pair => pair.Key)
            .Select(pair =>
            {
                var components = pair.Value.Components;
                var categorizedComponents = components
                    .Where(component => !string.Equals(component.Category, "General", StringComparison.Ordinal))
                    .ToArray();
                var usageBreakdown = categorizedComponents.Length > 0
                    ? categorizedComponents
                    : components.ToArray();
                var actualUsedBalls = categorizedComponents.Length > 0
                    ? categorizedComponents.Sum(component => component.Balls)
                    : pair.Value.DeclaredTotalBalls ?? usageBreakdown.Sum(component => component.Balls);

                return new OperationalDailyClosingDraftRequest
                {
                    CorrelationId = correlationId,
                    ClosingDate = pair.Key,
                    HistoricalWeeksToUse = historicalWeeksToUse,
                    ActualUsedBalls = actualUsedBalls,
                    UsageBreakdown = usageBreakdown,
                    Notes = pair.Value.Notes.Count == 0
                        ? $"Structured daily closing draft for {pair.Key:yyyy-MM-dd}."
                        : string.Join(' ', pair.Value.Notes),
                    ActorUserId = actorUserId
                };
            })
            .Where(request => request.ActualUsedBalls > 0)
            .ToArray();
    }

    private static IReadOnlyList<OperationalEventDraftRequest> FinalizeStructuredEventDrafts(
        IReadOnlyDictionary<DateOnly, EventAccumulator> accumulators,
        Guid correlationId,
        string actorUserId)
    {
        return accumulators
            .OrderBy(pair => pair.Key)
            .Where(pair =>
                !string.IsNullOrWhiteSpace(pair.Value.Name) &&
                pair.Value.ExpectedPeopleMinimum.HasValue &&
                pair.Value.ExpectedPeopleMaximum.HasValue &&
                pair.Value.EstimatedDoughBalls.HasValue)
            .Select(pair => new OperationalEventDraftRequest
            {
                CorrelationId = correlationId,
                EventDate = pair.Key,
                Name = pair.Value.Name!,
                EstimatedDoughBalls = pair.Value.EstimatedDoughBalls!.Value,
                ExpectedPeopleMinimum = pair.Value.ExpectedPeopleMinimum!.Value,
                ExpectedPeopleMaximum = pair.Value.ExpectedPeopleMaximum!.Value,
                PreviousNarrativeDoughBalls = pair.Value.PreviousNarrativeDoughBalls,
                AllowShortFermentation = false,
                Notes = string.Join(' ', pair.Value.Notes),
                ActorUserId = actorUserId
            })
            .ToArray();
    }

    private static bool TryExtractStructuredEventName(string content, out string eventName)
    {
        var fieldMatch = EventNameFieldPattern.Match(content);
        if (fieldMatch.Success)
        {
            eventName = fieldMatch.Groups["name"].Value.Trim();
            return !string.IsNullOrWhiteSpace(eventName);
        }

        var cleanedContent = StripStructuredPrefix(content);
        var eventMatch = EventNamePattern.Match(cleanedContent);
        if (!eventMatch.Success)
        {
            eventName = string.Empty;
            return false;
        }

        eventName = $"{eventMatch.Groups["name"].Value.Trim().TrimEnd(':', '.', ';')} event";
        return !string.IsNullOrWhiteSpace(eventName) &&
            !Regex.IsMatch(eventName, @"\d", RegexOptions.CultureInvariant);
    }

    private static string SanitizeUsageContent(string content)
    {
        return content
            .Replace("Farmers Market", "Farmers", StringComparison.OrdinalIgnoreCase)
            .Replace("Restaurant/general", "Restaurant", StringComparison.OrdinalIgnoreCase)
            .Replace("Restaurant / general", "Restaurant", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldIgnoreInventoryTransformationLine(string content)
    {
        var normalized = NormalizeForMatching(content);
        return normalized.Contains("no reballed", StringComparison.Ordinal) ||
            normalized.Contains("no reball", StringComparison.Ordinal) ||
            normalized.Contains("no discarded", StringComparison.Ordinal) ||
            normalized.Contains("no inventory correction", StringComparison.Ordinal) ||
            normalized.Contains("no source-trace", StringComparison.Ordinal) ||
            normalized.Contains("no source date", StringComparison.Ordinal) ||
            normalized.Contains("still missing", StringComparison.Ordinal);
    }

    private static OperationalChatDetectedIntent CreateStructuredDetectedIntent(
        string domain,
        string intentKind,
        string decision,
        DateOnly effectiveDate)
    {
        return new OperationalChatDetectedIntent
        {
            Domain = domain,
            IntentKind = intentKind,
            Summary = decision,
            Decision = decision,
            SourceFragment = domain,
            EffectiveDate = effectiveDate
        };
    }

    private static bool TryParseSupportedDate(string? value, out DateOnly parsedDate)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            return true;
        }

        return DateOnly.TryParseExact(value, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
    }

    private static string StripStructuredPrefix(string content)
    {
        var match = DayLinePattern.Match(content);
        return match.Success
            ? match.Groups["content"].Value.Trim()
            : content.Trim();
    }

    private static bool FailStructuredWeekSlice(
        out OperationalWeekSliceRequest weekSliceRequest,
        out IReadOnlyList<OperationalChatDetectedIntent> detectedIntents,
        out IReadOnlyList<OperationalValidationWarning> warnings)
    {
        weekSliceRequest = default!;
        detectedIntents = [];
        warnings = [];
        return false;
    }

    private static bool TryBuildProductionRequests(
        string segment,
        Guid correlationId,
        DateOnly effectiveDate,
        int historicalWeeksToUse,
        string actorUserId,
        out IReadOnlyList<OperationalDoughTaskDraftRequest> requests,
        out string prompt)
    {
        var normalizedSegment = NormalizeForMatching(segment);
        var mentionsBall = HasBallTaskCue(normalizedSegment);
        var mentionsLoad = HasMakeLoadCue(normalizedSegment);

        if (!mentionsBall && !mentionsLoad)
        {
            requests = [];
            prompt = "Add BallDough or MakeDoughLoad so the production line is explicit.";
            return false;
        }

        var hasBareGenericNumber = Regex.IsMatch(normalizedSegment, @"\b\d+\b", RegexOptions.CultureInvariant) &&
            !BallsPattern.IsMatch(normalizedSegment) &&
            !LoadsPattern.IsMatch(normalizedSegment);

        if (mentionsBall && mentionsLoad && hasBareGenericNumber)
        {
            requests = [];
            prompt = "When one line contains BallDough and MakeDoughLoad, specify balls or loads explicitly or split them into two lines.";
            return false;
        }

        var builtRequests = new List<OperationalDoughTaskDraftRequest>(2);
        var autoCompleteOnApproval = ShouldAutoCompleteProductionTask(normalizedSegment);
        if (mentionsBall)
        {
            var quantityValue = TryParseBalls(segment) ?? DoughRules.StandardBatchBalls;
            builtRequests.Add(new OperationalDoughTaskDraftRequest
            {
                CorrelationId = correlationId,
                TaskDate = effectiveDate,
                HistoricalWeeksToUse = historicalWeeksToUse,
                TaskType = nameof(PrepTaskType.BallDough),
                QuantityValue = quantityValue,
                QuantityUnit = nameof(DoughQuantityUnit.Balls),
                AssignedRole = nameof(ApplicationRole.PizzaMaker),
                AutoCompleteOnApproval = autoCompleteOnApproval,
                CompletionQuantityValue = autoCompleteOnApproval ? quantityValue : null,
                Notes = segment.Trim(),
                ActorUserId = actorUserId
            });
        }

        if (mentionsLoad)
        {
            var quantityValue = TryParseLoads(segment) ?? 1;
            builtRequests.Add(new OperationalDoughTaskDraftRequest
            {
                CorrelationId = correlationId,
                TaskDate = effectiveDate,
                HistoricalWeeksToUse = historicalWeeksToUse,
                TaskType = nameof(PrepTaskType.MakeDoughLoad),
                QuantityValue = quantityValue,
                QuantityUnit = nameof(DoughQuantityUnit.FullLoads),
                AssignedRole = nameof(ApplicationRole.PizzaMaker),
                AutoCompleteOnApproval = autoCompleteOnApproval,
                CompletionQuantityValue = autoCompleteOnApproval ? quantityValue : null,
                Notes = segment.Trim(),
                ActorUserId = actorUserId
            });
        }

        requests = builtRequests;
        prompt = string.Empty;
        return true;
    }

    private static bool TryBuildDailyClosingRequest(
        string segment,
        Guid correlationId,
        DateOnly effectiveDate,
        int historicalWeeksToUse,
        string actorUserId,
        out OperationalDailyClosingDraftRequest request,
        out string prompt)
    {
        var usageBreakdown = ParseUsageBreakdown(segment);
        if (usageBreakdown.Count == 0)
        {
            request = default!;
            prompt = "Add the used dough balls for the closing line, for example 'Tue: 90 balls' or 'Fri: Farmers 45 + Restaurant 150'.";
            return false;
        }

        request = new OperationalDailyClosingDraftRequest
        {
            CorrelationId = correlationId,
            ClosingDate = effectiveDate,
            HistoricalWeeksToUse = historicalWeeksToUse,
            ActualUsedBalls = usageBreakdown.Sum(component => component.Balls),
            UsageBreakdown = usageBreakdown,
            Notes = segment.Trim(),
            ActorUserId = actorUserId
        };
        prompt = string.Empty;
        return true;
    }

    private static bool TryBuildEventRequest(
        string segment,
        Guid correlationId,
        DateOnly effectiveDate,
        string actorUserId,
        out OperationalEventDraftRequest request,
        out string prompt)
    {
        if (!TryExtractStructuredEventName(segment, out var eventName))
        {
            request = default!;
            prompt = "Name the external event explicitly, for example 'Ted Vergakis event'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(eventName) || Regex.IsMatch(eventName, @"\d", RegexOptions.CultureInvariant))
        {
            request = default!;
            prompt = "Name the external event with a real event label, for example 'Ted Vergakis event'.";
            return false;
        }

        var peopleRangeMatch = PeopleRangePattern.Match(segment);
        if (!peopleRangeMatch.Success ||
            !int.TryParse(peopleRangeMatch.Groups["min"].Value, out var minimumPeople) ||
            !int.TryParse(peopleRangeMatch.Groups["max"].Value, out var maximumPeople))
        {
            request = default!;
            prompt = "Add the event people range, for example '51-75 people'.";
            return false;
        }

        var doughBalls = TryParseBalls(segment);
        if (!doughBalls.HasValue || doughBalls.Value <= 0)
        {
            request = default!;
            prompt = "Add the event dough balls used or planned, for example '55 balls'.";
            return false;
        }

        var previousNarrativeMatch = PreviousNarrativeBallsPattern.Match(segment);
        int? previousNarrativeBalls = previousNarrativeMatch.Success &&
            int.TryParse(previousNarrativeMatch.Groups["value"].Value, out var previousNarrativeValue)
                ? previousNarrativeValue
                : null;

        request = new OperationalEventDraftRequest
        {
            CorrelationId = correlationId,
            EventDate = effectiveDate,
            Name = eventName,
            EstimatedDoughBalls = doughBalls.Value,
            ExpectedPeopleMinimum = minimumPeople,
            ExpectedPeopleMaximum = maximumPeople,
            PreviousNarrativeDoughBalls = previousNarrativeBalls,
            AllowShortFermentation = false,
            Notes = segment.Trim(),
            ActorUserId = actorUserId
        };
        prompt = string.Empty;
        return true;
    }

    private static IReadOnlyList<OperationalUsageComponent> ParseUsageBreakdown(string segment)
    {
        var matches = CategoryBallsPattern.Matches(segment);
        if (matches.Count > 0)
        {
            return matches
                .Select(match => new OperationalUsageComponent(
                    NormalizeUsageCategory(match.Groups["category"].Value),
                    int.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture),
                    string.Equals(match.Groups["category"].Value, "event", StringComparison.OrdinalIgnoreCase)
                        ? "EmbeddedEventUsage"
                        : null))
                .ToArray();
        }

        var totalBalls = TryParseBalls(segment);
        if (!totalBalls.HasValue || totalBalls.Value <= 0)
        {
            return [];
        }

        return
        [
            new OperationalUsageComponent(
                "General",
                totalBalls.Value)
        ];
    }

    private static string NormalizeUsageCategory(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "farmer's" => "Farmers",
            "farmers" => "Farmers",
            "restaurant" => "Restaurant",
            "event" => "Event",
            "catering" => "Catering",
            "retail" => "Retail",
            "market" => "Market",
            _ => "General"
        };
    }

    private static bool IsWeeklyClosingPreviewSegment(string normalizedSegment)
    {
        return normalizedSegment.Contains("weekly closing", StringComparison.Ordinal) ||
            normalizedSegment.Contains("carryover", StringComparison.Ordinal) ||
            normalizedSegment.Contains("sobraron", StringComparison.Ordinal) ||
            normalizedSegment.Contains("lineas", StringComparison.Ordinal) ||
            normalizedSegment.Contains("preview", StringComparison.Ordinal);
    }

    private static bool IsEventSegment(string normalizedSegment)
    {
        if (PeopleRangePattern.IsMatch(normalizedSegment))
        {
            return true;
        }

        if (!normalizedSegment.Contains("event", StringComparison.Ordinal) &&
            !normalizedSegment.Contains("evento", StringComparison.Ordinal))
        {
            return false;
        }

        var match = EventNamePattern.Match(normalizedSegment);
        return match.Success &&
               !Regex.IsMatch(match.Groups["name"].Value, @"[\d+]", RegexOptions.CultureInvariant);
    }

    private static bool HasExplicitProductionCue(string normalizedSegment)
    {
        return HasBallTaskCue(normalizedSegment) || HasMakeLoadCue(normalizedSegment);
    }

    private static bool ShouldAutoCompleteProductionTask(string normalizedSegment)
    {
        return normalizedSegment.Contains("completed", StringComparison.Ordinal) ||
            normalizedSegment.Contains("complete", StringComparison.Ordinal) ||
            normalizedSegment.Contains("done", StringComparison.Ordinal) ||
            normalizedSegment.Contains("finished", StringComparison.Ordinal) ||
            normalizedSegment.Contains("terminado", StringComparison.Ordinal);
    }

    private static bool HasBallTaskCue(string normalizedSegment)
    {
        return normalizedSegment.Contains("balldough", StringComparison.Ordinal) ||
            normalizedSegment.Contains("ball dough", StringComparison.Ordinal) ||
            normalizedSegment.Contains("balling", StringComparison.Ordinal) ||
            normalizedSegment.Contains("balled", StringComparison.Ordinal) ||
            normalizedSegment.Contains("bole", StringComparison.Ordinal);
    }

    private static bool HasMakeLoadCue(string normalizedSegment)
    {
        return normalizedSegment.Contains("makedoughload", StringComparison.Ordinal) ||
            normalizedSegment.Contains("make dough load", StringComparison.Ordinal) ||
            normalizedSegment.Contains("make load", StringComparison.Ordinal) ||
            normalizedSegment.Contains("load", StringComparison.Ordinal) ||
            normalizedSegment.Contains("carga", StringComparison.Ordinal);
    }

    private static bool IsDailyClosingSegment(string normalizedSegment, OperationalIntent intent)
    {
        if (intent is SalesIntent)
        {
            return true;
        }

        if (intent is ConsumptionIntent consumptionIntent && !consumptionIntent.MentionsReball)
        {
            return true;
        }

        if (CategoryBallsPattern.IsMatch(normalizedSegment))
        {
            return true;
        }

        return BallsPattern.IsMatch(normalizedSegment) && !HasExplicitProductionCue(normalizedSegment);
    }

    private static bool IsInventoryTransformationSegment(string normalizedSegment)
    {
        return normalizedSegment.Contains("reball", StringComparison.Ordinal) ||
            normalizedSegment.Contains("rebole", StringComparison.Ordinal) ||
            normalizedSegment.Contains("discard", StringComparison.Ordinal) ||
            normalizedSegment.Contains("waste", StringComparison.Ordinal) ||
            normalizedSegment.Contains("merma", StringComparison.Ordinal) ||
            normalizedSegment.Contains("transform", StringComparison.Ordinal);
    }

    private static DateOnly ResolveSegmentDate(
        string segment,
        DateOnly referenceDate,
        DateOnly targetWeekStartDate)
    {
        var isoDateMatch = IsoDatePattern.Match(segment);
        if (isoDateMatch.Success &&
            DateOnly.TryParseExact(
                isoDateMatch.Groups["value"].Value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var explicitDate))
        {
            return explicitDate;
        }

        var normalizedSegment = NormalizeForMatching(segment);
        var weekStart = NormalizeWeekStart(targetWeekStartDate);

        foreach (var (offset, tokens) in WeekdayTokens)
        {
            if (tokens.Any(token => normalizedSegment.Contains(token, StringComparison.Ordinal)))
            {
                return weekStart.AddDays(offset);
            }
        }

        return referenceDate;
    }

    private static IReadOnlyList<string> SplitSegments(string sourceText)
    {
        var normalizedSourceText = sourceText.Replace("\r", string.Empty, StringComparison.Ordinal);
        var structuredSegments = TrySplitStructuredOperationalLog(normalizedSourceText);
        if (structuredSegments.Count > 0)
        {
            return structuredSegments;
        }

        return normalizedSourceText
            .Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => segment.Trim().TrimStart('-', '*'))
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();
    }

    private static IReadOnlyList<string> TrySplitStructuredOperationalLog(string sourceText)
    {
        var lines = sourceText
            .Split('\n', StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (lines.Length == 0 ||
            !lines.Any(TryGetWeekdayHeader) ||
            !lines.Any(IsBulletLine))
        {
            return [];
        }

        var segments = new List<string>(lines.Length);
        string? currentWeekdayHeader = null;

        foreach (var line in lines)
        {
            if (TryGetWeekdayHeader(line, out var weekdayHeader))
            {
                currentWeekdayHeader = weekdayHeader;
                continue;
            }

            var contentSegments = line
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(segment => segment.Trim().TrimStart('-', '*').Trim())
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .ToArray();

            foreach (var contentSegment in contentSegments)
            {
                segments.Add(currentWeekdayHeader is null
                    ? contentSegment
                    : $"{currentWeekdayHeader} {contentSegment}");
            }
        }

        return segments;
    }

    private static bool TryGetWeekdayHeader(string line)
    {
        return TryGetWeekdayHeader(line, out _);
    }

    private static bool TryGetWeekdayHeader(string line, out string weekdayHeader)
    {
        var trimmedLine = line.Trim();
        if (!trimmedLine.EndsWith(':'))
        {
            weekdayHeader = string.Empty;
            return false;
        }

        var headerCandidate = trimmedLine[..^1].Trim();
        var dayPrefixMatch = DayLinePattern.Match($"{headerCandidate}:");
        if (!dayPrefixMatch.Success || !string.IsNullOrWhiteSpace(dayPrefixMatch.Groups["content"].Value))
        {
            weekdayHeader = string.Empty;
            return false;
        }

        weekdayHeader = $"{headerCandidate}:";
        return true;
    }

    private static bool IsBulletLine(string line)
    {
        var trimmedLine = line.TrimStart();
        return trimmedLine.StartsWith("-", StringComparison.Ordinal) ||
            trimmedLine.StartsWith("*", StringComparison.Ordinal);
    }

    private static string NormalizeForMatching(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    private static int? TryParseBalls(string input)
    {
        var match = BallsPattern.Match(input);
        return match.Success && int.TryParse(match.Groups["value"].Value, out var value)
            ? value
            : null;
    }

    private static int? TryParseLoads(string input)
    {
        var match = LoadsPattern.Match(input);
        return match.Success && int.TryParse(match.Groups["value"].Value, out var value)
            ? value
            : null;
    }

    private static DateOnly NormalizeWeekStart(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private static string ResolveActorUserId(string? actorUserId)
    {
        return string.IsNullOrWhiteSpace(actorUserId)
            ? "operational-chat"
            : actorUserId.Trim();
    }

    private static bool HasBlockingWarnings(IReadOnlyList<OperationalValidationWarning> warnings)
    {
        return warnings.Any(warning => warning.BlocksDraft);
    }

    private static IReadOnlyList<OperationalValidationWarning> MergeWarnings(IEnumerable<OperationalValidationWarning> warnings)
    {
        return warnings
            .GroupBy(warning => $"{warning.Code}|{warning.Message}", StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    private static OperationalChatResponse BuildClarificationResponse(
        Guid correlationId,
        string sourceText,
        IReadOnlyList<OperationalChatDetectedIntent> detectedIntents,
        IReadOnlyList<OperationalValidationWarning> warnings,
        string clarificationPrompt)
    {
        return new OperationalChatResponse
        {
            CorrelationId = correlationId,
            SourceText = sourceText.Trim(),
            NarrativeSummary = "No drafts were created yet.",
            RequiresClarification = true,
            ClarificationPrompt = clarificationPrompt,
            DetectedIntents = detectedIntents,
            Warnings = warnings
        };
    }

    private static bool IsRecoverable(Exception exception)
    {
        return exception is ArgumentException or InvalidOperationException or KeyNotFoundException;
    }

    private static readonly IReadOnlyList<(int Offset, string[] Tokens)> WeekdayTokens =
    [
        (0, ["mon", "monday", "lunes"]),
        (1, ["tue", "tues", "tuesday", "martes"]),
        (2, ["wed", "wednesday", "miercoles"]),
        (3, ["thu", "thurs", "thursday", "jueves"]),
        (4, ["fri", "friday", "viernes"]),
        (5, ["sat", "saturday", "sabado"]),
        (6, ["sun", "sunday", "domingo"])
    ];

    private sealed class SegmentAnalysis
    {
        public OperationalChatDetectedIntent DetectedIntent { get; init; } = new();

        public bool RequiresClarification { get; init; }

        public IReadOnlyList<OperationalValidationWarning> InitialWarnings { get; init; } = [];

        public IReadOnlyList<DraftPlan> Plans { get; init; } = [];

        public Func<CancellationToken, Task<OperationalSimulationResult>>? SimulateForAuditAsync { get; init; }
    }

    private sealed class DraftPlan
    {
        public DraftPlan(
            OperationalChatDetectedIntent detectedIntent,
            Func<CancellationToken, Task<OperationalSimulationResult>> simulateAsync,
            Func<CancellationToken, Task<OperationalDraftEnvelope>> createDraftAsync)
        {
            DetectedIntent = detectedIntent;
            SimulateAsync = simulateAsync;
            CreateDraftAsync = createDraftAsync;
        }

        public OperationalChatDetectedIntent DetectedIntent { get; }

        public Func<CancellationToken, Task<OperationalSimulationResult>> SimulateAsync { get; }

        public Func<CancellationToken, Task<OperationalDraftEnvelope>> CreateDraftAsync { get; }
    }

    private enum StructuredOperationalDomain
    {
        None,
        Production,
        DailyClosing,
        ExternalEvent,
        InventoryTransformation,
        WeeklyClosingPreview
    }

    private sealed class DailyClosingAccumulator
    {
        public DailyClosingAccumulator(DateOnly date)
        {
            Date = date;
        }

        public DateOnly Date { get; }

        public List<OperationalUsageComponent> Components { get; } = [];

        public List<string> Notes { get; } = [];

        public int? DeclaredTotalBalls { get; set; }
    }

    private sealed class EventAccumulator
    {
        public EventAccumulator(DateOnly date)
        {
            Date = date;
        }

        public DateOnly Date { get; }

        public string? Name { get; set; }

        public int? ExpectedPeopleMinimum { get; set; }

        public int? ExpectedPeopleMaximum { get; set; }

        public int? EstimatedDoughBalls { get; set; }

        public int? PreviousNarrativeDoughBalls { get; set; }

        public List<string> Notes { get; } = [];
    }
}
