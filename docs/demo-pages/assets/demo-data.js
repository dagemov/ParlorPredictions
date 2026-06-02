(function () {
  const STORAGE_KEY = "parlorPredictionDemoState";
  const SESSION_KEY = "parlorPredictionDemoSession";
  const DEMO_CREDENTIALS = {
    email: "pizzamaker@parlor.local",
    password: "demo123",
    name: "Marco Rivera",
    role: "Pizza Maker",
  };

  const BALLS_PER_CASE = 12;
  const BALLS_PER_LOAD = 168;
  const CASES_PER_LOAD = 14;

  const DEFAULT_STATE = {
    startingInventoryBalls: 96,
    needsByDay: [
      { date: "2026-06-02", label: "Tuesday", restaurantBalls: 60, eventBalls: 0 },
      { date: "2026-06-03", label: "Wednesday", restaurantBalls: 72, eventBalls: 0 },
      { date: "2026-06-04", label: "Thursday", restaurantBalls: 96, eventBalls: 48 },
      { date: "2026-06-05", label: "Friday", restaurantBalls: 210, eventBalls: 36 },
      { date: "2026-06-06", label: "Saturday", restaurantBalls: 220, eventBalls: 60 },
      { date: "2026-06-07", label: "Sunday", restaurantBalls: 80, eventBalls: 0 },
    ],
    tasks: [
      {
        id: "task-1",
        date: "2026-06-02",
        title: "Tuesday dough prep",
        type: "balls",
        quantity: 60,
        equivalentBalls: 60,
        status: "completed",
        notes: "Covered lunch service and the early dough pull.",
        completedBy: "Sebastian Martinez",
        completedAt: "2026-06-02T08:05:00",
      },
      {
        id: "task-2",
        date: "2026-06-03",
        title: "Wednesday prep load",
        type: "loads",
        quantity: 1,
        equivalentBalls: 168,
        status: "pending",
        notes: "Supports Wednesday and early Thursday service.",
        completedBy: null,
        completedAt: null,
      },
      {
        id: "task-3",
        date: "2026-06-04",
        title: "Thursday market support",
        type: "balls",
        quantity: 84,
        equivalentBalls: 84,
        status: "pending",
        notes: "Prepare extra dough for the farmers market pull.",
        completedBy: null,
        completedAt: null,
      },
      {
        id: "task-4",
        date: "2026-06-05",
        title: "Friday full batch",
        type: "loads",
        quantity: 1,
        equivalentBalls: 168,
        status: "pending",
        notes: "Carry dough into the Friday night rush.",
        completedBy: null,
        completedAt: null,
      },
      {
        id: "task-5",
        date: "2026-06-06",
        title: "Saturday top-up",
        type: "balls",
        quantity: 96,
        equivalentBalls: 96,
        status: "pending",
        notes: "Top up for weekend walk-ins and reservations.",
        completedBy: null,
        completedAt: null,
      },
    ],
    calendarEvents: [
      {
        id: "event-1",
        date: "2026-06-02",
        title: "Dough prep",
        category: "Dough Prep",
        description: "Mix dough for Tuesday service and pull older dough first.",
      },
      {
        id: "event-2",
        date: "2026-06-03",
        title: "Sauce prep",
        category: "Sauce Prep",
        description: "Fresh marinara batch for midweek service.",
      },
      {
        id: "event-3",
        date: "2026-06-04",
        title: "Inventory check",
        category: "Operations",
        description: "Review flour, yeast, and box count before the weekend.",
      },
      {
        id: "event-4",
        date: "2026-06-05",
        title: "Weekend rush forecast",
        category: "Forecast",
        description: "Expect heavier Friday dinner volume and market traffic.",
      },
      {
        id: "event-5",
        date: "2026-06-06",
        title: "Staff prep shift",
        category: "Team",
        description: "Early prep shift focused on dough and line setup.",
      },
      {
        id: "event-6",
        date: "2026-06-06",
        title: "Private patio event",
        category: "Event",
        description: "Reserve 60 dough balls for the private patio dinner.",
      },
    ],
  };

  function clone(value) {
    return JSON.parse(JSON.stringify(value));
  }

  function loadState() {
    const raw = localStorage.getItem(STORAGE_KEY);

    if (!raw) {
      const initialState = clone(DEFAULT_STATE);
      saveState(initialState);
      return initialState;
    }

    try {
      return JSON.parse(raw);
    } catch (error) {
      const recoveredState = clone(DEFAULT_STATE);
      saveState(recoveredState);
      return recoveredState;
    }
  }

  function saveState(state) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }

  function resetState() {
    const initialState = clone(DEFAULT_STATE);
    saveState(initialState);
    return initialState;
  }

  function getSession() {
    const raw = localStorage.getItem(SESSION_KEY);
    return raw ? JSON.parse(raw) : null;
  }

  function setSession(session) {
    localStorage.setItem(SESSION_KEY, JSON.stringify(session));
  }

  function clearSession() {
    localStorage.removeItem(SESSION_KEY);
  }

  function login(email, password) {
    const normalizedEmail = String(email || "").trim().toLowerCase();
    if (
      normalizedEmail === DEMO_CREDENTIALS.email &&
      String(password || "") === DEMO_CREDENTIALS.password
    ) {
      setSession({
        email: DEMO_CREDENTIALS.email,
        name: DEMO_CREDENTIALS.name,
        role: DEMO_CREDENTIALS.role,
        loggedInAt: new Date().toISOString(),
      });
      return { ok: true };
    }

    return {
      ok: false,
      message: "Use pizzamaker@parlor.local with password demo123 for this static demo.",
    };
  }

  function ensureAuthenticated() {
    const requiresAuth = document.body.dataset.requiresAuth === "true";
    if (!requiresAuth) {
      return;
    }

    if (!getSession()) {
      window.location.href = "./login.html";
    }
  }

  function formatNumber(value) {
    return new Intl.NumberFormat("en-US").format(value);
  }

  function formatLoadsFromBalls(balls) {
    return (balls / BALLS_PER_LOAD).toFixed(1);
  }

  function formatShortDate(dateValue) {
    const date = new Date(dateValue + "T12:00:00");
    return new Intl.DateTimeFormat("en-US", {
      weekday: "short",
      month: "short",
      day: "numeric",
    }).format(date);
  }

  function getTaskStatusBadge(status) {
    if (status === "completed") {
      return "success";
    }
    return "warning";
  }

  function getSummary(state) {
    const totalNeededBalls = state.needsByDay.reduce(function (sum, day) {
      return sum + day.restaurantBalls + day.eventBalls;
    }, 0);

    const finishedBalls = state.tasks.reduce(function (sum, task) {
      return sum + (task.status === "completed" ? task.equivalentBalls : 0);
    }, 0);

    const remainingBalls = Math.max(
      totalNeededBalls - finishedBalls - state.startingInventoryBalls,
      0
    );

    const completedTasks = state.tasks.filter(function (task) {
      return task.status === "completed";
    }).length;

    const pendingTasks = state.tasks.filter(function (task) {
      return task.status === "pending";
    }).length;

    return {
      totalNeededBalls: totalNeededBalls,
      finishedBalls: finishedBalls,
      remainingBalls: remainingBalls,
      loadsNeeded: formatLoadsFromBalls(totalNeededBalls),
      loadsCompleted: formatLoadsFromBalls(finishedBalls),
      upcomingPrepEvents: state.calendarEvents.length,
      completedTasks: completedTasks,
      pendingTasks: pendingTasks,
      startingInventoryBalls: state.startingInventoryBalls,
    };
  }

  function getDayStatus(day, completedBalls) {
    const totalNeeded = day.restaurantBalls + day.eventBalls;
    const stillMissing = Math.max(totalNeeded - completedBalls, 0);

    if (totalNeeded === 0) {
      return "No Data";
    }

    if (stillMissing === 0) {
      return "Covered";
    }

    if (completedBalls > 0) {
      return "In Progress";
    }

    if (day.eventBalls > 0) {
      return "Event Ahead";
    }

    return "Needs Dough";
  }

  function getWeekRows(state) {
    return state.needsByDay.map(function (day) {
      const completedBalls = state.tasks.reduce(function (sum, task) {
        if (task.date === day.date && task.status === "completed") {
          return sum + task.equivalentBalls;
        }
        return sum;
      }, 0);

      const totalNeeded = day.restaurantBalls + day.eventBalls;
      const stillMissing = Math.max(totalNeeded - completedBalls, 0);

      return {
        date: day.date,
        label: day.label,
        restaurantBalls: day.restaurantBalls,
        eventBalls: day.eventBalls,
        totalNeeded: totalNeeded,
        completedBalls: completedBalls,
        stillMissing: stillMissing,
        status: getDayStatus(day, completedBalls),
      };
    });
  }

  function completeTask(taskId) {
    const state = loadState();
    const task = state.tasks.find(function (item) {
      return item.id === taskId;
    });

    if (!task || task.status === "completed") {
      return loadState();
    }

    const session = getSession();

    task.status = "completed";
    task.completedBy = session ? session.name : "Demo User";
    task.completedAt = new Date().toISOString();
    saveState(state);
    return state;
  }

  function bindCommonUi() {
    const session = getSession();
    const userBadge = document.querySelector("[data-demo-user]");
    const logoutButton = document.querySelector("[data-demo-logout]");
    const resetButton = document.querySelector("[data-demo-reset]");

    if (userBadge && session) {
      userBadge.textContent = session.name + " · " + session.role;
    }

    if (logoutButton) {
      logoutButton.addEventListener("click", function () {
        clearSession();
        window.location.href = "./login.html";
      });
    }

    if (resetButton) {
      resetButton.addEventListener("click", function () {
        resetState();
        window.location.reload();
      });
    }
  }

  function renderDashboard() {
    const state = loadState();
    const summary = getSummary(state);
    const weekRows = getWeekRows(state);

    document.querySelector("[data-dashboard-needed]").textContent = formatNumber(summary.totalNeededBalls);
    document.querySelector("[data-dashboard-finished]").textContent = formatNumber(summary.finishedBalls);
    document.querySelector("[data-dashboard-remaining]").textContent = formatNumber(summary.remainingBalls);
    document.querySelector("[data-dashboard-loads-needed]").textContent = summary.loadsNeeded;
    document.querySelector("[data-dashboard-loads-completed]").textContent = summary.loadsCompleted;
    document.querySelector("[data-dashboard-events]").textContent = formatNumber(summary.upcomingPrepEvents);
    document.querySelector("[data-dashboard-copy]").textContent =
      "This week starts with " +
      formatNumber(summary.startingInventoryBalls) +
      " dough balls already available. The team has already finished " +
      formatNumber(summary.finishedBalls) +
      " dough balls, which leaves " +
      formatNumber(summary.remainingBalls) +
      " still to cover.";

    const weekTable = document.querySelector("[data-dashboard-week]");
    weekTable.innerHTML = weekRows
      .map(function (row) {
        return (
          "<tr>" +
          "<td>" + row.label + "</td>" +
          "<td>" + formatNumber(row.restaurantBalls) + "</td>" +
          "<td>" + formatNumber(row.eventBalls) + "</td>" +
          "<td>" + formatNumber(row.totalNeeded) + "</td>" +
          "<td>" + formatNumber(row.completedBalls) + "</td>" +
          "<td>" + formatNumber(row.stillMissing) + "</td>" +
          "<td><span class='status-pill status-" + row.status.toLowerCase().replace(/\s+/g, "-") + "'>" + row.status + "</span></td>" +
          "</tr>"
        );
      })
      .join("");

    const eventsList = document.querySelector("[data-dashboard-events-list]");
    eventsList.innerHTML = state.calendarEvents
      .slice(0, 4)
      .map(function (eventItem) {
        return (
          "<div class='demo-list-item'>" +
          "<div>" +
          "<strong>" + eventItem.title + "</strong>" +
          "<div class='muted-copy'>" + formatShortDate(eventItem.date) + " · " + eventItem.category + "</div>" +
          "</div>" +
          "<span class='badge rounded-pill text-bg-light'>" + eventItem.category + "</span>" +
          "</div>"
        );
      })
      .join("");
  }

  function renderDoughPage() {
    const state = loadState();
    const summary = getSummary(state);
    const weekRows = getWeekRows(state);

    document.querySelector("[data-dough-summary]").textContent =
      "The team needs " +
      formatNumber(summary.totalNeededBalls) +
      " dough balls this week. " +
      formatNumber(summary.finishedBalls) +
      " are already finished, and " +
      formatNumber(summary.remainingBalls) +
      " are still open.";

    document.querySelector("[data-dough-week-needed]").textContent = formatNumber(summary.totalNeededBalls);
    document.querySelector("[data-dough-week-finished]").textContent = formatNumber(summary.finishedBalls);
    document.querySelector("[data-dough-week-remaining]").textContent = formatNumber(summary.remainingBalls);
    document.querySelector("[data-dough-loads-finished]").textContent = summary.loadsCompleted;

    const taskRows = document.querySelector("[data-dough-tasks]");
    taskRows.innerHTML = state.tasks
      .map(function (task) {
        const statusBadge = getTaskStatusBadge(task.status);
        const completeButton =
          task.status === "pending"
            ? "<button class='btn btn-sm btn-accent' data-complete-task='" + task.id + "'>Mark completed</button>"
            : "<span class='text-success fw-semibold'>Done by " + task.completedBy + "</span>";

        return (
          "<tr>" +
          "<td>" + formatShortDate(task.date) + "</td>" +
          "<td>" + task.title + "</td>" +
          "<td class='text-capitalize'>" + task.type + "</td>" +
          "<td>" + formatNumber(task.quantity) + "</td>" +
          "<td>" + formatNumber(task.equivalentBalls) + " dough balls</td>" +
          "<td><span class='badge text-bg-" + statusBadge + " text-capitalize'>" + task.status + "</span></td>" +
          "<td>" + completeButton + "</td>" +
          "</tr>"
        );
      })
      .join("");

    const previewList = document.querySelector("[data-dough-preview]");
    previewList.innerHTML = state.tasks
      .filter(function (task) {
        return task.status === "pending";
      })
      .slice(0, 3)
      .map(function (task) {
        return (
          "<li><strong>" +
          task.title +
          "</strong> counts as " +
          formatNumber(task.equivalentBalls) +
          " dough balls when completed.</li>"
        );
      })
      .join("");

    const statusCards = document.querySelector("[data-dough-week-grid]");
    statusCards.innerHTML = weekRows
      .map(function (row) {
        return (
          "<div class='col-md-6 col-xl-4'>" +
          "<div class='demo-card mini-day-card h-100'>" +
          "<div class='d-flex justify-content-between align-items-start gap-3'>" +
          "<div>" +
          "<div class='eyebrow'>" + row.label + "</div>" +
          "<h3 class='h5 mb-1'>" + formatShortDate(row.date) + "</h3>" +
          "</div>" +
          "<span class='status-pill status-" + row.status.toLowerCase().replace(/\s+/g, "-") + "'>" + row.status + "</span>" +
          "</div>" +
          "<div class='row row-cols-2 g-3 mt-1'>" +
          "<div><div class='metric-label'>Needed</div><div class='metric-inline'>" + formatNumber(row.totalNeeded) + "</div></div>" +
          "<div><div class='metric-label'>Finished</div><div class='metric-inline'>" + formatNumber(row.completedBalls) + "</div></div>" +
          "<div><div class='metric-label'>Restaurant</div><div class='metric-inline'>" + formatNumber(row.restaurantBalls) + "</div></div>" +
          "<div><div class='metric-label'>Events</div><div class='metric-inline'>" + formatNumber(row.eventBalls) + "</div></div>" +
          "</div>" +
          "<p class='muted-copy mb-0 mt-3'>Still missing: " + formatNumber(row.stillMissing) + " dough balls.</p>" +
          "</div>" +
          "</div>"
        );
      })
      .join("");

    document.querySelectorAll("[data-complete-task]").forEach(function (button) {
      button.addEventListener("click", function () {
        completeTask(button.getAttribute("data-complete-task"));
        renderDoughPage();
      });
    });
  }

  function renderCalendarPage() {
    const state = loadState();
    const weekRows = getWeekRows(state);
    const eventsByDate = {};

    state.calendarEvents.forEach(function (eventItem) {
      if (!eventsByDate[eventItem.date]) {
        eventsByDate[eventItem.date] = [];
      }
      eventsByDate[eventItem.date].push(eventItem);
    });

    const weekSummary = getSummary(state);
    document.querySelector("[data-calendar-summary]").textContent =
      "This week includes " +
      formatNumber(state.calendarEvents.length) +
      " prep events and " +
      formatNumber(weekSummary.pendingTasks) +
      " dough tasks still open.";

    const calendarGrid = document.querySelector("[data-calendar-grid]");
    calendarGrid.innerHTML = weekRows
      .map(function (row) {
        const eventsMarkup = (eventsByDate[row.date] || [])
          .map(function (eventItem) {
            return (
              "<div class='calendar-chip'>" +
              "<strong>" + eventItem.title + "</strong>" +
              "<div class='muted-copy small'>" + eventItem.description + "</div>" +
              "</div>"
            );
          })
          .join("");

        return (
          "<div class='col-lg-4 col-md-6'>" +
          "<div class='demo-card calendar-day-card h-100'>" +
          "<div class='d-flex justify-content-between align-items-start gap-2 mb-3'>" +
          "<div>" +
          "<div class='eyebrow'>" + row.label + "</div>" +
          "<h3 class='h5 mb-0'>" + formatShortDate(row.date) + "</h3>" +
          "</div>" +
          "<span class='status-pill status-" + row.status.toLowerCase().replace(/\s+/g, "-") + "'>" + row.status + "</span>" +
          "</div>" +
          "<div class='calendar-metrics'>" +
          "<div><span>Restaurant dough</span><strong>" + formatNumber(row.restaurantBalls) + "</strong></div>" +
          "<div><span>Event dough</span><strong>" + formatNumber(row.eventBalls) + "</strong></div>" +
          "<div><span>Total needed</span><strong>" + formatNumber(row.totalNeeded) + "</strong></div>" +
          "<div><span>Finished</span><strong>" + formatNumber(row.completedBalls) + "</strong></div>" +
          "<div><span>Still missing</span><strong>" + formatNumber(row.stillMissing) + "</strong></div>" +
          "</div>" +
          "<div class='calendar-event-stack mt-3'>" +
          (eventsMarkup || "<div class='muted-copy'>No extra prep events planned for this day.</div>") +
          "</div>" +
          "</div>" +
          "</div>"
        );
      })
      .join("");
  }

  function renderLoginPage() {
    const form = document.querySelector("[data-login-form]");
    const errorBox = document.querySelector("[data-login-error]");

    if (!form) {
      return;
    }

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      const email = form.querySelector("[name='email']").value;
      const password = form.querySelector("[name='password']").value;
      const result = login(email, password);

      if (!result.ok) {
        errorBox.textContent = result.message;
        errorBox.classList.remove("d-none");
        return;
      }

      window.location.href = "./dashboard.html";
    });
  }

  function renderLandingPage() {
    const session = getSession();
    const continueLink = document.querySelector("[data-continue-link]");

    if (session && continueLink) {
      continueLink.href = "./dashboard.html";
      continueLink.textContent = "Continue to the dashboard";
    }
  }

  function initPage() {
    ensureAuthenticated();
    bindCommonUi();

    const page = document.body.dataset.page;

    if (page === "login") {
      renderLoginPage();
      return;
    }

    if (page === "landing") {
      renderLandingPage();
      return;
    }

    if (page === "dashboard") {
      renderDashboard();
      return;
    }

    if (page === "dough") {
      renderDoughPage();
      return;
    }

    if (page === "calendar") {
      renderCalendarPage();
    }
  }

  window.ParlorPredictionDemo = {
    BALLS_PER_CASE: BALLS_PER_CASE,
    BALLS_PER_LOAD: BALLS_PER_LOAD,
    CASES_PER_LOAD: CASES_PER_LOAD,
    DEMO_CREDENTIALS: DEMO_CREDENTIALS,
    login: login,
    completeTask: completeTask,
    getSession: getSession,
    clearSession: clearSession,
    getState: loadState,
    resetState: resetState,
    getSummary: function () {
      return getSummary(loadState());
    },
    getWeekRows: function () {
      return getWeekRows(loadState());
    },
  };

  document.addEventListener("DOMContentLoaded", initPage);
})();
