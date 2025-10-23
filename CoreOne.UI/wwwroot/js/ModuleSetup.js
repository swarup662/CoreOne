


// Open edit modal
function openEditModal(menuId) {
    fetch(`/ModuleSetup/GetMenuForEdit?id=${menuId}`)

        .then(res => res.ok ? res.json() : Promise.reject("Failed to load menu"))
        .then(data => {
            if (!data) throw new Error("No data returned");

            // Populate menu fields
            $("#hdnMenuModuleID").val(data.menuID);
            $("#MenuName").val(data.menuName);
            $("#MenuSeq").val(data.menuSeq);
            $("#MenuSymbol").val(data.menuSymbol);

            const tbody = $("#moduleTable tbody").empty();

            if (data.modules && data.modules.length > 0) {
                // Sort modules by sequence ascending
                const sortedModules = data.modules.sort((a, b) => a.sequence - b.sequence);

                sortedModules.forEach(m => {
                    // Check for duplicate sequence in table
                    const seqExists = $("#moduleTable tbody tr").filter(function () {
                        return parseInt($(this).find(".moduleSeq input").val()) === m.sequence;
                    }).length > 0;

                    if (!seqExists) {
                        tbody.append(`
                                    <tr data-module-id="${m.moduleID}">
                                        <td class="moduleId" style="display:none">${m.moduleID}</td>
                                        <td class="moduleSeq">
                                            <input type="number" class="form-control form-control-sm" value="${m.sequence}" />
                                        </td>
                                        <td class="moduleName">
                                            <input type="text" class="form-control form-control-sm" value="${m.name}" />
                                        </td>
                                        <td class="moduleUrl">
                                            <input type="text" class="form-control form-control-sm" value="${m.url}" />
                                        </td>
                                         <td class="d-flex gap-1 justify-content-start">
       
            <a href="javascript:void(0);"  onclick="ActionSetup('${m.moduleID}')" style="background: #f75964;color: white;" class="btn btn-sm setupModule" title="Setup"><i class="fe-command"></i></a>
            </td>
                                    </tr>
                                `);
                    }
                });
            }
            $('#moduleModalLabel').text('Update Modules Setup :: Edit');
            $('#moduleSetupModal').attr('aria-labelledby', 'moduleModalLabel');
            $("#moduleSetupModal").modal("show");
        })
        .catch(err => alert(err));
}




// Add module to table
$("#addModuleBtn").click(function () {
    const name = $("#ModuleName").val().trim();
    const url = $("#ModuleUrl").val().trim();
    const seq = $("#ModuleSeq").val().trim();

    // Validate empty fields
    if (!name || !url || !seq) {
        Swal.fire({
            icon: 'error',
            title: 'Validation Error',
            text: 'Please enter Module Name, URL, and Sequence.'
        });
        return;
    }

    // Check for duplicate sequence
    const seqExists = $("#moduleTable tbody tr").filter(function () {
        return parseInt($(this).find(".moduleSeq input").val()) === parseInt(seq);
    }).length > 0;

    if (seqExists) {
        Swal.fire({
            icon: 'error',
            title: 'Validation Error',
            text: 'Sequence number already exists. Please use a unique sequence.'
        });
        return;
    }

    // Create new row with input fields
    const row = `
                <tr data-module-id="">
                    <td class="moduleId" style="display:none"></td>
                    <td class="moduleSeq"><input type="number" class="form-control form-control-sm" value="${seq}" /></td>
                    <td class="moduleName"><input type="text" class="form-control form-control-sm" value="${name}" /></td>
                    <td class="moduleUrl"><input type="text" class="form-control form-control-sm" value="${url}" /></td>
              <td class="d-flex gap-1 justify-content-start">
            <a href="javascript:void(0);" class="btn btn-sm btn-danger removeModule" title="Remove">
                <i class="fe-trash"></i>
            </a>
            <a href="javascript:void(0);" class="btn btn-sm btn-primary setupModule" title="Setup">
                <i class="fe-settings"></i>
            </a>
        </td>
                </tr>`;

    // Insert row in ascending sequence order
    let inserted = false;
    $("#moduleTable tbody tr").each(function () {
        const currentSeq = parseInt($(this).find(".moduleSeq input").val());
        if (parseInt(seq) < currentSeq) {
            $(this).before(row);
            inserted = true;
            return false;
        }
    });

    if (!inserted) {
        $("#moduleTable tbody").append(row);
    }

    // Clear input fields
    $("#ModuleName, #ModuleUrl, #ModuleSeq").val('');
});



// Remove module button
$(document).on("click", ".removeModule", function () {
    $(this).closest("tr").remove();
});


// Map input IDs to model property names
const fieldMap = {
    "MenuName": "Name",
    "MenuSeq": "Sequence",
    "MenuSymbol": "MenuSymbol"
};





// When user clicks an icon
$("#iconList .icon-item").click(function () {
    // Get the icon name or identifier (for example, use class)
    const iconClass = $(this).find("i").attr("class"); // e.g., "fe-home"

    // Set the input value
    $("#MenuSymbol").val(iconClass);

    // Trigger validation for MenuSymbol
    const menuId = $("#hdnMenuModuleID").val() || null;
    validateMenuField("MenuSymbol", menuId);
});





// Validate Menu fields with server AND client empty check
function validateMenuField(fieldId, menuId = null) {
    let $input = $("#" + fieldId);
    let $errorSpan = $("#" + fieldId + "-error");
    let value = $input.val().trim();

    // Clear previous state
    $input.removeClass("is-invalid is-valid");
    $errorSpan.text('');

    // Client-side empty check
    if (!value) {
        $input.addClass("is-invalid");
        $errorSpan.text("This field is required.");
        return $.Deferred().resolve().promise(); // Stop AJAX, already invalid
    }

    // Server-side validation
    let payload = {};
    payload[fieldId] = value;
    if (menuId) payload["MenuModuleID"] = menuId;

    return $.ajax({
        url: '/ModuleSetup/ValidateMenuField',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (resp) {
            const modelField = fieldMap[fieldId];
            if (resp.errors && resp.errors[modelField]) {
                $input.addClass("is-invalid");
                $errorSpan.text(resp.errors[modelField][0]);
            } else {
                $input.addClass("is-valid");
            }
        }
    });
}




// Real-time validation
$("#MenuName, #MenuSeq, #MenuSymbol").on("input change", function () {
    const menuId = $("#hdnMenuModuleID").val() || null;
    validateMenuField(this.id, menuId);
});




// Save Menu + Modules
$("#btnSavemenu").click(async function () {
    const menuId = $("#hdnMenuModuleID").val();

    // Validate menu fields first
    // Validate menu fields first
    await validateMenuField("MenuName", menuId);
    await validateMenuField("MenuSeq", menuId);
    await validateMenuField("MenuSymbol", menuId);

    // Prevent save if validation fails
    if ($("#MenuName").hasClass("is-invalid") ||
        $("#MenuSeq").hasClass("is-invalid") ||
        $("#MenuSymbol").hasClass("is-invalid")) {


        return; // stop execution
    }

    // Check if at least one module exists
    const moduleCount = $("#moduleTable tbody tr").length;
    if (moduleCount === 0) {
        Swal.fire({
            icon: 'error',
            title: 'Validation Error',
            text: 'Please add at least one module before saving.'
        });
        return; // stop execution
    }

    // Continue to build payload and save...


    // Build payload
    const menuWithModules = {
        MenuModuleID: menuId ? parseInt(menuId) : null,
        Name: $("#MenuName").val().trim(),
        MenuSymbol: $("#MenuSymbol").val().trim(),
        Sequence: parseInt($("#MenuSeq").val()),
        RecType: menuId ? 'U' : 'I',
        Modules: []
    };

    // Collect modules (read from input fields)
    $("#moduleTable tbody tr").each(function () {
        const row = $(this);
        const moduleId = row.data("module-id");

        menuWithModules.Modules.push({
            ModuleID: moduleId ? parseInt(moduleId) : null,
            Name: row.find(".moduleName input").val().trim(),
            Url: row.find(".moduleUrl input").val().trim(),
            Sequence: parseInt(row.find(".moduleSeq input").val())
        });
    });


    // Send save request
    try {
        const resp = await fetch("/ModuleSetup/SaveMenuWithModules", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(menuWithModules)
        });

        if (!resp.ok) {
            const errText = await resp.text();
            throw new Error(errText);
        }

        const result = await resp.json();
        Swal.fire({
            icon: 'success',
            title: 'Success!',
            text: 'Menu & Modules saved successfully.',
            timer: 2000,
            showConfirmButton: false
        }).then(() => {
            $("#moduleSetupModal").modal("hide");
            location.reload();
        });

    } catch (err) {
        console.error(err);
        Swal.fire({
            icon: 'error',
            title: 'Error!',
            text: 'Error saving menu/modules: ' + err.message
        });
    }
});



