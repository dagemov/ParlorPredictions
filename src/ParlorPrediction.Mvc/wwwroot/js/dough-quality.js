(() => {
  const syncTaskTypeUnits = (root = document) => {
    root.querySelectorAll("[data-dough-task-type]").forEach((taskTypeInput) => {
      const form = taskTypeInput.closest("form");
      if (!form || form.dataset.doughTaskTypeBound === "true") {
        return;
      }

      const taskTypeSelect = taskTypeInput.matches("select") ? taskTypeInput : null;
      const unitSelect = form.querySelector("[data-dough-quantity-unit]");
      if (!(unitSelect instanceof HTMLSelectElement) || !(taskTypeSelect instanceof HTMLSelectElement)) {
        form.dataset.doughTaskTypeBound = "true";
        return;
      }

      const sync = () => {
        if (taskTypeSelect.value === "MakeDoughLoad") {
          unitSelect.innerHTML = "<option value=\"FullLoads\">Full Loads</option>";
          unitSelect.value = "FullLoads";
          return;
        }

        if (!unitSelect.querySelector("option[value='Balls']")) {
          unitSelect.innerHTML = `
            <option value="Balls">Dough Balls</option>
            <option value="Cases">Cases</option>
            <option value="FullLoads">Full Loads</option>`;
        }

        if (!unitSelect.value) {
          unitSelect.value = "Balls";
        }
      };

      taskTypeSelect.addEventListener("change", sync);
      sync();
      form.dataset.doughTaskTypeBound = "true";
    });
  };

  document.addEventListener("click", (event) => {
    const trigger = event.target instanceof Element
      ? event.target.closest("[data-dq-print]")
      : null;

    if (!trigger) {
      return;
    }

    event.preventDefault();
    window.print();
  });

  document.addEventListener("DOMContentLoaded", () => {
    syncTaskTypeUnits(document);
  });

  document.body.addEventListener("htmx:afterSwap", (event) => {
    syncTaskTypeUnits(event.target);
  });
})();
