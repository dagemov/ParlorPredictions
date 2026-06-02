(() => {
  const BALLS_PER_CASE = 12;
  const BALLS_PER_FULL_LOAD = 168;

  const buildQuantityPreviewText = (mode, unit, quantityValue) => {
    const quantity = Number.parseInt(quantityValue ?? "", 10);
    if (!unit || !Number.isFinite(quantity) || quantity <= 0) {
      switch (mode) {
        case "planned":
          return "Choose a quantity type and amount to preview the dough planned for this task.";
        case "recommended":
          return "Choose a quantity type and amount to preview the dough covered by this recommendation.";
        default:
          return "Choose a completion type and quantity to preview the dough balls total.";
      }
    }

    let balls;
    switch (unit) {
      case "Cases":
        balls = quantity * BALLS_PER_CASE;
        break;
      case "FullLoads":
        balls = quantity * BALLS_PER_FULL_LOAD;
        break;
      default:
        balls = quantity;
        break;
    }

    switch (mode) {
      case "planned":
        return `This task will plan ${balls} dough balls for the kitchen.`;
      case "recommended":
        return `This manager note will recommend ${balls} dough balls.`;
      default:
        return `This will count as ${balls} dough balls completed.`;
    }
  };

  const refreshDoughQuantityPreview = (form) => {
    if (!form) {
      return;
    }

    const preview = form.querySelector("[data-dough-quantity-preview]");
    if (!preview) {
      return;
    }

    const unitInput = form.querySelector("[data-dough-quantity-unit]");
    const quantityInput = form.querySelector("[data-dough-quantity-value]");
    const mode = form.dataset.doughQuantityPreviewMode ?? "completed";

    preview.textContent = buildQuantityPreviewText(
      mode,
      unitInput?.value,
      quantityInput?.value
    );
  };

  const wireDoughQuantityPreview = (root = document) => {
    root.querySelectorAll("[data-dough-quantity-form]").forEach((form) => {
      if (form.dataset.doughQuantityBound === "true") {
        refreshDoughQuantityPreview(form);
        return;
      }

      const refresh = () => refreshDoughQuantityPreview(form);
      form.querySelectorAll("[data-dough-quantity-unit], [data-dough-quantity-value]").forEach((input) => {
        input.addEventListener("input", refresh);
        input.addEventListener("change", refresh);
      });

      form.dataset.doughQuantityBound = "true";
      refresh();
    });
  };

  const showAlert = (detail) => {
    if (!window.Swal) {
      return;
    }

    const icon = detail?.type ?? "info";
    const title = detail?.title ?? "Update";
    const text = detail?.message ?? "";

    window.Swal.fire({
      icon,
      title,
      text,
      confirmButtonColor: "#1f6feb"
    });
  };

  document.body.addEventListener("prepAlert", (event) => {
    showAlert(event.detail);
  });

  document.body.addEventListener("htmx:configRequest", (event) => {
    const form = event.detail.elt?.closest?.("form");
    const tokenInput =
      form?.querySelector?.('input[name="__RequestVerificationToken"]') ??
      document.querySelector('input[name="__RequestVerificationToken"]');

    if (tokenInput?.value) {
      event.detail.headers.RequestVerificationToken = tokenInput.value;
    }
  });

  document.body.addEventListener("htmx:confirm", (event) => {
    const element = event.detail.elt;
    if (!element || !element.matches("[data-swal-confirm]") || !window.Swal) {
      return;
    }

    event.preventDefault();

    window.Swal.fire({
      icon: "question",
      title: element.dataset.confirmTitle ?? "Continue?",
      text: element.dataset.confirmText ?? "Please confirm this action.",
      showCancelButton: true,
      confirmButtonColor: "#1f6feb",
      cancelButtonColor: "#98a2b3",
      confirmButtonText: "Confirm"
    }).then((result) => {
      if (result.isConfirmed) {
        event.detail.issueRequest(true);
      }
    });
  });

  document.body.addEventListener("htmx:afterSwap", (event) => {
    wireDoughQuantityPreview(event.target);
  });

  wireDoughQuantityPreview();
})();
