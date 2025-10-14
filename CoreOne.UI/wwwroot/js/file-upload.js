let tempBlobCommonId = null;
let zoomLevel = 1;

// Open modal
function openFileUpload(moduleName) {

    const modal = new bootstrap.Modal(document.getElementById("fileUploadModal"));
    document.getElementById("uploadModule").value = moduleName;

    clearPreview();
    document.getElementById("uploadFileInput").value = "";
    tempBlobCommonId = null;

    modal.show();
}

// Clear previous preview
function clearPreview() {
    const iframe = document.getElementById("filePreviewFrame");
    iframe.src = "";
    document.getElementById("previewSection").style.display = "none";
    zoomLevel = 1;

    const errorDiv = document.getElementById("uploadError");
    if (errorDiv) {
        errorDiv.innerText = "";
        errorDiv.style.display = "none";
    }

    clearValidation();

    const downloadBtn = document.getElementById("downloadBtn");
    if (downloadBtn) downloadBtn.remove();
}

// Validate file on change
document.getElementById("uploadFileInput").addEventListener("change", async function () {
    const file = this.files[0];
    clearValidation();

    if (!file) return;

    const module = document.getElementById("uploadModule").value;
    const resp = await fetch(`/api/FileUpload/getModuleConfig?module=${module}`);
    const config = await resp.json();

    const ext = "." + file.name.split('.').pop().toLowerCase();

    if (!config.AllowedExtensions.includes(ext)) {
        showValidationError(`Extension '${ext}' not allowed`);
        this.value = "";
        return;
    }

    if (file.size > config.MaxSizeInMB * 1024 * 1024) {
        showValidationError(`File size exceeded (${config.MaxSizeInMB} MB max)`);
        this.value = "";
        return;
    }

    showValidationSuccess();
});


// ===== Custom upload UI behavior =====
const uploadBox = document.getElementById("uploadBox");
const uploadInput = document.getElementById("uploadFileInput");
const fileNameDisplay = document.getElementById("fileNameDisplay");
const fileUploadModal = document.getElementById("fileUploadModal");

// Clicking box opens file selector
uploadBox.addEventListener("click", () => uploadInput.click());

// Show selected filename
uploadInput.addEventListener("change", () => {
    if (uploadInput.files && uploadInput.files.length > 0) {
        const fileName = uploadInput.files[0].name;
        fileNameDisplay.textContent = `Selected: ${fileName}`;
    } else {
        fileNameDisplay.textContent = "";
    }
});

// Drag & Drop highlight
["dragenter", "dragover"].forEach(evt => {
    uploadBox.addEventListener(evt, e => {
        e.preventDefault();
        uploadBox.classList.add("dragover");
    });
});

["dragleave", "drop"].forEach(evt => {
    uploadBox.addEventListener(evt, e => {
        e.preventDefault();
        uploadBox.classList.remove("dragover");
    });
});

// Handle dropped file
uploadBox.addEventListener("drop", e => {
    e.preventDefault();
    if (e.dataTransfer.files.length > 0) {
        uploadInput.files = e.dataTransfer.files;
        const fileName = uploadInput.files[0].name;
        fileNameDisplay.textContent = `Selected: ${fileName}`;
    }
});

// ===== Clear on modal close =====
fileUploadModal.addEventListener("hidden.bs.modal", () => {
    uploadInput.value = "";
    fileNameDisplay.textContent = "";
});

// ===== Clear after successful upload (optional, safe hook) =====
// Call this inside your existing AJAX success or upload complete logic
function clearUploadFileDisplay() {
    uploadInput.value = "";
    fileNameDisplay.textContent = "";
}







// Temp upload
document.getElementById("uploadBtn").addEventListener("click", async () => {
    const file = document.getElementById("uploadFileInput").files[0];
    if (!file) {
        showValidationError("Please select a file.");
        return;
    }

    const module = document.getElementById("uploadModule").value;
    const formData = new FormData();
    formData.append("module", module);
    formData.append("file", file);

    const resp = await fetch("/api/FileUpload/tempUpload", { method: "POST", body: formData });
    const result = await resp.json();

    if (!resp.ok) {
        showValidationError(result.error || "Upload failed");
        return;
    }

    tempBlobCommonId = result.tempId;
    showPreview(tempBlobCommonId, result.contentType);
    showValidationSuccess();
});

// Show preview & download button
function showPreview(tempId, contentType) {
    const iframe = document.getElementById("filePreviewFrame");
    const previewSection = document.getElementById("previewSection");

    // Reset
    iframe.style.display = "none";
    iframe.src = "";

    let placeholder = document.getElementById("previewPlaceholder");
    if (!placeholder) {
        placeholder = document.createElement("div");
        placeholder.id = "previewPlaceholder";
        placeholder.style.padding = "20px";
        placeholder.innerHTML = "<p>No preview available</p>";
        previewSection.appendChild(placeholder);
    }
    placeholder.style.display = "none";

    // Show content
    if (contentType.startsWith("image/") || contentType === "application/pdf") {
        iframe.style.display = "block";
        iframe.src = `/api/FileUpload/viewTemp?tempId=${tempId}&inline=true` +
            (contentType === "application/pdf" ? "#toolbar=0&navpanes=0&scrollbar=0" : "");
    } else {
        placeholder.style.display = "block";
    }

    // Show preview section
    previewSection.style.display = "block";

    // Download button
    let downloadBtn = document.getElementById("downloadBtn");
    if (!downloadBtn) {
        downloadBtn = document.createElement("a");
        downloadBtn.id = "downloadBtn";
        downloadBtn.className = "btn btn-primary mt-2";
        downloadBtn.textContent = "Download File";
        previewSection.appendChild(downloadBtn);
    }
    downloadBtn.href = `/api/FileUpload/viewTemp?tempId=${tempId}&inline=false`;
    downloadBtn.setAttribute("download", "");
    downloadBtn.setAttribute("target", "_blank");
}

// Zoom
document.getElementById("zoomInBtn").addEventListener("click", () => {
    zoomLevel += 0.1;
    document.getElementById("filePreviewFrame").style.transform = `scale(${zoomLevel})`;
});

document.getElementById("zoomOutBtn").addEventListener("click", () => {
    if (zoomLevel > 0.2) zoomLevel -= 0.1;
    document.getElementById("filePreviewFrame").style.transform = `scale(${zoomLevel})`;
});

// Validation helpers
function showValidationError(msg) {
    const input = document.getElementById("uploadFileInput");
    const errorDiv = document.getElementById("uploadError");
    errorDiv.innerText = msg;
    errorDiv.style.display = "block";
    input.classList.remove("is-valid");
    input.classList.add("is-invalid");
}

function showValidationSuccess() {
    const input = document.getElementById("uploadFileInput");
    const errorDiv = document.getElementById("uploadError");
    errorDiv.innerText = "";
    errorDiv.style.display = "none";
    input.classList.remove("is-invalid");
    input.classList.add("is-valid");
}

function clearValidation() {
    const input = document.getElementById("uploadFileInput");
    const errorDiv = document.getElementById("uploadError");
    input.classList.remove("is-invalid", "is-valid");
    errorDiv.innerText = "";
    errorDiv.style.display = "none";
}

// Save temp file to disk
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

    try {
        const resp = await fetch("/api/FileUpload/save", {
            method: "POST",
            body: formData
        });

        const result = await resp.json();

        if (!resp.ok) {
            return {
                success: false,
                id: "SAVE_FAILED",
                message: result.error || "Failed to save file to server.",
                fileName: null,
                relativePath: null
            };
        }

        // ✅ Success
        return {
            success: true,
            id: null,
            message: "File saved successfully.",
            fileName: result.fileName,
            relativePath: result.relativePath
        };

    } catch (err) {
        return {
            success: false,
            id: "NETWORK_OR_UNKNOWN",
            message: err.message || "An unknown error occurred while saving the file.",
            fileName: null,
            relativePath: null
        };
    }
}




// Preview any file in edit mode with download button
// Show file in separate view modal
async function prepareSavedFilePreview(filePath) {
    const iframe = document.getElementById("viewFileFrame");
    const placeholder = document.getElementById("viewFilePlaceholder");
    const downloadBtn = document.getElementById("downloadFileBtn");
    const previewSection = document.getElementById("viewFilePreviewSection");
    const loadingIndicator = document.getElementById("viewFileLoading"); // optional spinner div

    // Reset previous state
    iframe.style.display = "none";
    iframe.src = "";
    placeholder.style.display = "none";
    previewSection.style.display = "none";
    if (loadingIndicator) loadingIndicator.style.display = "block";

    const ext = filePath.split('.').pop().toLowerCase();
    const imageExts = ["jpg", "jpeg", "png", "gif"];
    const pdfExts = ["pdf"];

    try {
        const resp = await fetch(`/api/FileUpload/viewSaved?filePath=${encodeURIComponent(filePath)}`);
        if (!resp.ok) throw new Error("File not found");

        const blob = await resp.blob();
        const blobUrl = URL.createObjectURL(blob);

        // Clear previous preview
        iframe.src = "";
        placeholder.innerHTML = "";

        if (imageExts.includes(ext)) {
            iframe.style.display = "block";
            iframe.src = blobUrl;
            iframe.classList.add("file-preview-frame"); // CSS class for styling
        } else if (pdfExts.includes(ext)) {
            iframe.style.display = "block";
            iframe.src = blobUrl + "#toolbar=0&navpanes=0&scrollbar=0";
            iframe.classList.add("file-preview-frame");
        } else {
            placeholder.style.display = "flex";
            placeholder.innerHTML = `<p class="text-muted">No preview available for this file type.</p>`;
        }

        // Set download link
        downloadBtn.href = blobUrl;
        downloadBtn.setAttribute("download", filePath.split("/").pop());
        downloadBtn.setAttribute("target", "_blank");

        // Show section and hide loader
        if (loadingIndicator) loadingIndicator.style.display = "none";
        previewSection.style.display = "block";
        document.getElementById("zoomInBtnView").addEventListener("click", () => {
            zoomLevel += 0.1;
            document.getElementById("viewFileFrame").style.transform = `scale(${zoomLevel})`;
        });

        document.getElementById("zoomOutBtnView").addEventListener("click", () => {
            if (zoomLevel > 0.2) zoomLevel -= 0.1;
            document.getElementById("viewFileFrame").style.transform = `scale(${zoomLevel})`;
        });



        // Modal display handled elsewhere (eye icon click)
    } catch (err) {
        console.error("Preview error:", err);
        if (loadingIndicator) loadingIndicator.style.display = "none";
        placeholder.style.display = "flex";
        placeholder.innerHTML = `<p class="text-danger">Unable to preview file.</p>`;
        previewSection.style.display = "block";
    }
}


// Eye icon click handler
document.querySelectorAll(".eye-icon").forEach(icon => {
    icon.addEventListener("click", function () {
        
    });
});


function openFileViewModalshow() {
    const modal = new bootstrap.Modal(document.getElementById("viewFileModal"));
    modal.show();
}


