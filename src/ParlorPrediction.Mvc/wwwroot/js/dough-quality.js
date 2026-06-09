(() => {
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
})();
