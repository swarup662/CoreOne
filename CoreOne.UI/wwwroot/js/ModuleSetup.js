


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







// ------------------------------------------------- Action Setup ---------------------------------------------------------- //



function ActionSetup(moduleID) {
    fetch(`/ModuleSetup/GetActionsForDropdown?moduleID=${moduleID}`)
        .then(response => response.json())
        .then(data => {
            console.log(data); // should show array of objects with id and name

            const dropdown = document.getElementById('actionDropdown');
            dropdown.innerHTML = '<option value="">--Select Action--</option>';

            data.forEach(action => {
                const option = document.createElement('option');
                option.value = action.id;   // <-- lowercase
                option.text = action.name;  // <-- lowercase
                dropdown.appendChild(option);
            });

            // Show modal
            const modal = new bootstrap.Modal(document.getElementById('actionSetupModal'));
            modal.show();

            dropdown.setAttribute('data-module-id', moduleID);
        })
        .catch(err => console.error('Error fetching actions:', err));
}

let selectedActionsList = []; // Store {ActionID, ActionName, ModuleId} objects

function ActionSetup(moduleID) {
    const dropdown = document.getElementById('actionDropdown');
    const selectedContainer = document.getElementById('selectedActions');

    dropdown.innerHTML = '<option value="">--Select Action--</option>';
    selectedContainer.innerHTML = '';
    selectedActionsList = [];

    fetch(`/ModuleSetup/GetActionsForDropdown?moduleID=${moduleID}`)
        .then(resp => resp.json())
        .then(allActions => {
            return fetch(`/ModuleSetup/GetModuleActions?moduleID=${moduleID}`)
                .then(resp => resp.json())
                .then(existingActions => {
                    existingActions = existingActions.map(a => ({
                        ActionID: a.ActionID || a.id,
                        ActionName: a.ActionName || a.name
                    }));

                    // ✅ Add existing DB actions (EDIT MODE)
                    existingActions.forEach(action => {
                        addActionButton(action.ActionID, action.ActionName, moduleID, false);
                    });

                    // ✅ Fill dropdown excluding selected
                    allActions.forEach(action => {
                        const id = action.ActionID || action.id;
                        const name = action.ActionName || action.name;

                        if (!selectedActionsList.find(a => a.ActionID === id)) {
                            const option = document.createElement('option');
                            option.value = id;
                            option.text = name;
                            dropdown.appendChild(option);
                        }
                    });

                    const modal = new bootstrap.Modal(document.getElementById('actionSetupModal'));
                    modal.show();
                    dropdown.setAttribute('data-module-id', moduleID);
                });
        })
        .catch(err => console.error('Error fetching actions:', err));
}

// ✅ Helper function to create button
function addActionButton(actionID, actionName, moduleID, isNew = true) {
    if (selectedActionsList.find(a => a.ActionID === actionID)) return;

    selectedActionsList.push({
        ActionID: parseInt(actionID),
        ActionName: actionName,
        ModuleId: parseInt(moduleID)
    });

    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'btn btn-sm btn-outline-primary m-1';

    // ✅ Add only for newly added actions
    if (isNew) {
        btn.innerHTML = `${actionName} <span class="text-danger fw-bold">✖</span>`;
        btn.classList.add("addFlag");
    } else {
        btn.innerText = actionName;
        btn.classList.add("editFlag");
    }

    btn.setAttribute('data-id', actionID);

    btn.addEventListener('click', function () {
        if (!isNew) return; // ❌ Prevent removing existing items

        // ✅ Sweet alert for removal
        Swal.fire({
            title: 'Remove this action?',
            text: actionName,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Remove',
            cancelButtonText: 'Cancel'
        }).then(result => {
            if (result.isConfirmed) {
                selectedActionsList = selectedActionsList.filter(a => a.ActionID != actionID);
                btn.remove();

                const dropdown = document.getElementById('actionDropdown');
                const option = document.createElement('option');
                option.value = actionID;
                option.text = actionName;
                dropdown.appendChild(option);
            }
        });
    });

    document.getElementById('selectedActions').appendChild(btn);
}

// ✅ Add action from dropdown
document.getElementById('addActionBtn').addEventListener('click', function () {
    const dropdown = document.getElementById('actionDropdown');
    const selectedOption = dropdown.options[dropdown.selectedIndex];

    const actionID = selectedOption.value;
    const actionName = selectedOption.text;
    const moduleID = dropdown.getAttribute('data-module-id');

    if (!actionID) {
        Swal.fire("Please select an action first!", "", "warning");
        return;
    }

    addActionButton(actionID, actionName, moduleID, true);
    selectedOption.remove();
    dropdown.selectedIndex = 0;
});

// ✅ Save to API
document.getElementById('saveActionBtn').addEventListener('click', function () {
    if (selectedActionsList.length === 0) {
        Swal.fire("No actions selected!", "Add at least one action.", "warning");
        return;
    }

    const moduleID = parseInt(document.getElementById('actionDropdown').getAttribute('data-module-id'));
    const createdBy = parseInt(document.getElementById('currentUserId')?.value || "1");

    const payload = {
        ModuleID: moduleID,
        Actions: selectedActionsList,
        CreatedBy: createdBy
    };

    fetch(`/ModuleSetup/SaveModuleActions`, {
        method: 'POST',
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(resp => resp.json())
        .then(result => {
            if (result.success) {
                Swal.fire("Saved!", "Actions saved successfully!", "success");
                const modal = bootstrap.Modal.getInstance(document.getElementById('actionSetupModal'));
                modal.hide();
            } else {
                Swal.fire("Failed!", result.message || "Could not save actions", "error");
            }
        })
        .catch(err => {
            console.error(err);
            Swal.fire("Error!", "Unexpected error occurred", "error");
        });
});


//-------------------------------====-- Action Setup ----------------------------------------------- //








