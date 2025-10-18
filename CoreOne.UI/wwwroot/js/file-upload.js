// =========================
// file-upload.js - Clean Full Version
// =========================

let tempBlobCommonId = null;
let zoomLevel = 1;
let ignoreChange = false; // flag to prevent recursion

// =========================
// OPEN FILE UPLOAD MODAL
// =========================
function openFileUpload(moduleName) {
    const $modal = $("#fileUploadModal");
    $("#uploadModule").val(moduleName);

    clearPreview();
    $("#uploadFileInput").val("");
    $("#fileNameDisplay").text("");
    tempBlobCommonId = null;

    $modal.modal("show");
}

// =========================
// CLEAR PREVIEW
// =========================
function clearPreview() {
    const $iframe = $("#filePreviewFrame");
    $iframe.attr("src", "").css("transform", "scale(1)");
    $("#previewSection").hide();
    zoomLevel = 1;

    $("#uploadError").text("").hide();
    clearValidation();
    $("#downloadBtn").remove();
}

// =========================
// VALIDATION HELPERS
// =========================
function showValidationError(msg) {
    const $input = $("#uploadFileInput");
    const $errorDiv = $("#uploadError");
    $errorDiv.text(msg).show();
    $input.removeClass("is-valid").addClass("is-invalid");
}

function showValidationSuccess() {
    const $input = $("#uploadFileInput");
    const $errorDiv = $("#uploadError");
    $errorDiv.text("").hide();
    $input.removeClass("is-invalid").addClass("is-valid");
}

function clearValidation() {
    const $input = $("#uploadFileInput");
    const $errorDiv = $("#uploadError");
    $input.removeClass("is-invalid is-valid");
    $errorDiv.text("").hide();
}

// =========================
// FILE INPUT CHANGE HANDLER
// =========================
$("#uploadFileInput").on("change", function () {
    const file = this.files[0];
    clearValidation();

    if (!file) {
        $fileNameDisplay.text("");
        return;
    }

    const module = $("#uploadModule").val();



});


// =========================
// CUSTOM UPLOAD UI
// =========================
const $uploadBox = $("#uploadBox");
const $uploadInput = $("#uploadFileInput");
const $fileNameDisplay = $("#fileNameDisplay");
const $fileUploadModal = $("#fileUploadModal");

// Click box to open file selector
$uploadBox.on("click", () => $("#uploadFileInput").click());
// Drag & drop highlight
["dragenter", "dragover"].forEach(evt => {
    $uploadBox.on(evt, function (e) {
        e.preventDefault();
        $uploadBox.addClass("dragover");
    });
});

["dragleave", "drop"].forEach(evt => {
    $uploadBox.on(evt, function (e) {
        e.preventDefault();
        $uploadBox.removeClass("dragover");
    });
});

// Handle dropped file
$uploadBox.on("drop", function (e) {
    e.preventDefault();
    const files = e.originalEvent.dataTransfer.files;
    if (files.length > 0) {
        $uploadInput[0].files = files;
        $fileNameDisplay.text(`Selected: ${files[0].name}`);
        $uploadInput.trigger("change"); // trigger validation
    }
});

// =========================
// CLEAR ON MODAL CLOSE
// =========================
$fileUploadModal.on("hidden.bs.modal", function () {
    $uploadInput.val("");
    $fileNameDisplay.text("");
    clearPreview();
});

// =========================
// TEMP UPLOAD
// =========================
$("#uploadBtn").on("click", function () {
    const file = $uploadInput[0].files[0];
    if (!file) {
        showValidationError("Please select a file.");
        return;
    }

    const module = $("#uploadModule").val();
    const formData = new FormData();
    formData.append("module", module);
    formData.append("file", file);

    $.ajax({
        url: "/api/FileUpload/tempUpload",
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        success: function (result) {
            tempBlobCommonId = result.tempId;
            showPreview(tempBlobCommonId, result.contentType);
            showValidationSuccess();
        },
        error: function (xhr) {
            const result = xhr.responseJSON || {};
            showValidationError(result.error || "Upload failed");
        }
    });
});

// =========================
// SHOW PREVIEW & DOWNLOAD
// =========================
function showPreview(tempId, contentType) {
    const $iframe = $("#filePreviewFrame");
    const $previewSection = $("#previewSection");
    let $placeholder = $("#previewPlaceholder");

    $iframe.hide().attr("src", "");
    if ($placeholder.length) $placeholder.hide();

    if (contentType.startsWith("image/") || contentType === "application/pdf") {
        $iframe.show().attr("src",
            `/api/FileUpload/viewTemp?tempId=${tempId}&inline=true` +
            (contentType === "application/pdf" ? "#toolbar=0&navpanes=0&scrollbar=0" : "")
        );
    } else {
        if (!$placeholder.length) {
            $previewSection.append("<div id='previewPlaceholder' style='padding:20px'><p>No preview available</p></div>");
            $placeholder = $("#previewPlaceholder");
        } else {
            $placeholder.show();
        }
    }


    $previewSection.show();

    let $downloadBtn = $("#downloadBtn");
    if (!$downloadBtn.length) {
        $downloadBtn = $("<a>", {
            id: "downloadBtn",
            class: "btn btn-primary mt-2",
            text: "Download File"
        });
        $previewSection.append($downloadBtn);
    }
    $downloadBtn.attr({
        href: `/api/FileUpload/viewTemp?tempId=${tempId}&inline=false`,
        download: "",
        target: "_blank"
    });
}

// =========================
// ZOOM BUTTONS
// =========================
$("#zoomInBtn").on("click", function () {
    zoomLevel += 0.1;
    $("#filePreviewFrame").css("transform", `scale(${zoomLevel})`);
});

$("#zoomOutBtn").on("click", function () {
    if (zoomLevel > 0.2) zoomLevel -= 0.1;
    $("#filePreviewFrame").css("transform", `scale(${zoomLevel})`);
});

// =========================
// SAVE TEMP FILE TO SERVER
// =========================
async function saveFile(tempId = tempBlobCommonId) {
    if (!tempId) {
        return {
            success: false,
            id: "NO_TEMP_FILE",
            message: "No temporary file available to save.",
            fileName: null,
            relativePath: null
        };
    }

    const formData = new FormData();
    formData.append("tempId", tempId);

    return new Promise((resolve) => {
        $.ajax({
            url: "/api/FileUpload/save",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function (result) {
                resolve({
                    success: true,
                    id: null,
                    message: "File saved successfully.",
                    fileName: result.fileName,
                    relativePath: result.relativePath
                });
            },
            error: function (xhr) {
                const result = xhr.responseJSON || {};
                resolve({
                    success: false,
                    id: "SAVE_FAILED",
                    message: result.error || "Failed to save file to server.",
                    fileName: null,
                    relativePath: null
                });
            }
        });
    });
}

// =========================
// PREVIEW SAVED FILE
// =========================
function prepareSavedFilePreview(filePath) {
    const $iframe = $("#viewFileFrame");
    const $placeholder = $("#viewFilePlaceholder");
    const $downloadBtn = $("#downloadFileBtn");
    const $previewSection = $("#viewFilePreviewSection");
    const $loading = $("#viewFileLoading");

    $iframe.hide().attr("src", "");
    $placeholder.hide();
    $previewSection.hide();
    if ($loading.length) $loading.show();

    const ext = filePath.split('.').pop().toLowerCase();
    const imageExts = ["jpg", "jpeg", "png", "gif"];
    const pdfExts = ["pdf"];

    $.ajax({
        url: `/api/FileUpload/viewSaved?filePath=${encodeURIComponent(filePath)}`,
        type: "GET",
        xhrFields: { responseType: "blob" },
        success: function (blob) {
            const blobUrl = URL.createObjectURL(blob);

            if (imageExts.includes(ext)) {
                $iframe.show().attr("src", blobUrl);
            } else if (pdfExts.includes(ext)) {
                $iframe.show().attr("src", blobUrl + "#toolbar=0&navpanes=0&scrollbar=0");
            } else {
                $placeholder.html(`<p class="text-muted">No preview available for this file type.</p>`).show();
            }

            $downloadBtn.attr({
                href: blobUrl,
                download: filePath.split("/").pop(),
                target: "_blank"
            });

            if ($loading.length) $loading.hide();
            $previewSection.show();

            $("#zoomInBtnView").off("click").on("click", function () {
                zoomLevel += 0.1;
                $iframe.css("transform", `scale(${zoomLevel})`);
            });

            $("#zoomOutBtnView").off("click").on("click", function () {
                if (zoomLevel > 0.2) zoomLevel -= 0.1;
                $iframe.css("transform", `scale(${zoomLevel})`);
            });
        },
        error: function () {
            if ($loading.length) $loading.hide();
            $placeholder.html(`<p class="text-danger">Unable to preview file.</p>`).show();
            $previewSection.show();
        }
    });
}

// =========================
// OPEN VIEW MODAL
// =========================
function openFileViewModalshow() {
    $("#viewFileModal").modal("show");
}
