(() => {
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
})();
