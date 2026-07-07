// ServiceBridge - shared browser behavior for Razor Pages.

(() => {
    window.serviceBridgeDownloadCsv = (fileName, content) => {
        const blob = new Blob([content], { type: "text/csv;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName || "admin-financial-report.csv";
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    };

    const ready = (callback) => {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", callback);
            return;
        }

        callback();
    };

    ready(() => {
        document.documentElement.classList.add("js-enabled");

        document.querySelectorAll("[data-sb-toast]").forEach((element) => {
            bootstrap.Toast.getOrCreateInstance(element, { delay: 4500 }).show();
        });

        const sidebarToggle = document.querySelector("[data-sidebar-toggle]");
        if (sidebarToggle) {
            sidebarToggle.addEventListener("click", () => {
                document.body.classList.toggle("sb-sidebar-collapsed");
            });
        }

        const modalElement = document.getElementById("sb-confirmation-modal");
        const messageElement = document.getElementById("sb-confirmation-message");
        const continueButton = document.getElementById("sb-confirmation-continue");
        if (!modalElement || !messageElement || !continueButton) {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
        let pendingForm = null;
        let pendingSubmitter = null;

        document.addEventListener("submit", (event) => {
            const submitter = event.submitter;
            const message = submitter?.getAttribute("data-confirm");
            if (!message || event.target.dataset.sbConfirmed === "true") {
                return;
            }

            event.preventDefault();
            pendingForm = event.target;
            pendingSubmitter = submitter;
            messageElement.textContent = message;
            modal.show();
        });

        continueButton.addEventListener("click", () => {
            if (!pendingForm) {
                return;
            }

            pendingForm.dataset.sbConfirmed = "true";
            modal.hide();
            pendingForm.requestSubmit(pendingSubmitter);
            pendingForm = null;
            pendingSubmitter = null;
        });

        modalElement.addEventListener("hidden.bs.modal", () => {
            if (pendingForm) {
                delete pendingForm.dataset.sbConfirmed;
            }

            pendingForm = null;
            pendingSubmitter = null;
        });
    });
})();
