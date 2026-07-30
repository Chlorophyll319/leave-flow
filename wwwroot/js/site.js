// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
  var modal = document.getElementById('cancelConfirmModal');
  if (!modal) {
    return;
  }

  var targetForm = null;
  var closeButton = modal.querySelector('[data-cancel-modal-close]');
  var confirmButton = modal.querySelector('[data-cancel-modal-confirm]');

  document.addEventListener('click', function (event) {
    var trigger = event.target.closest('[data-cancel-modal-trigger]');
    if (trigger) {
      targetForm = trigger.closest('form');
      modal.showModal();
    }
  });

  closeButton.addEventListener('click', function () {
    modal.close();
  });

  confirmButton.addEventListener('click', function (event) {
    event.currentTarget.disabled = true;
    if (targetForm) {
      targetForm.submit();
    }
  });

  modal.addEventListener('click', function (event) {
    if (event.target === modal) {
      modal.close();
    }
  });
})();
