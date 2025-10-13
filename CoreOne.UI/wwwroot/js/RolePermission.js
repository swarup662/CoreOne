let currentRoleId = 0;
let isAddMode = false;

// Add Mode
function openAddPermissionModal() {
    isAddMode = true;
    currentRoleId = 0;

    $("#roleDropdown").val("").prop("disabled", false);
    $("#permissionsContainer").html("<p class='text-muted'>Choose a role to load permissions...</p>");
    $("#permissionModal").modal("show");
    $("#selectAllGlobal").prop("checked", false);
}

// Manage Mode
function openPermissionModal(roleId, roleName) {
    isAddMode = false;
    currentRoleId = roleId;
   
    $("#roleDropdown").val(roleId);
    $("#permissionsContainer").html("<p class='text-muted'>Loading...</p>");
    $("#permissionModal").modal("show");
    $("#selectAllGlobal").prop("checked", false);

    fetchPermissions(roleId);
}

// View Mode
function ViewPermissionModal(roleId, roleName) {
    isAddMode = false;
    currentRoleId = roleId;
    $("#roleDropdown").val(roleId);
    $("#SaveRolePermissions").remove();
    $("#permissionsContainer").html("<p class='text-muted'>Loading...</p>");
    $("#permissionModal").modal("show");
    $("#selectAllGlobal").prop("checked", false);

    fetchPermissions(roleId);
}


// Dropdown change
function onRoleChange() {
    let roleId = $("#roleDropdown").val();
    if (roleId) {
        currentRoleId = roleId;
        $("#permissionsContainer").html("<p class='text-muted'>Loading...</p>");
        fetchPermissions(roleId);
    } else {
        $("#permissionsContainer").html("<p class='text-muted'>Choose a role to load permissions...</p>");
    }
}


function fetchPermissions(roleId) {
    $("#permissionsContainer").html("<p class='text-muted'>Loading...</p>");

    $.ajax({
        url: `/RolePermission/GetRolePermissions?roleId=${roleId}`,
        type: "GET",
        success: function (html) {
            $("#permissionsContainer").html(html);

            // Wire up events after injecting HTML
            $(".select-all").on("change", function () {
                let module = $(this).data("module");
                let isChecked = $(this).is(":checked");
                $(`.perm-checkbox[data-module='${module}']`).prop("checked", isChecked);
                updateGlobalSelectAll();
            });

            $(".perm-checkbox").on("change", function () {
                let module = $(this).data("module");
                let allChecked = $(`.perm-checkbox[data-module='${module}']`).length ===
                    $(`.perm-checkbox[data-module='${module}']:checked`).length;
                $(`.select-all[data-module='${module}']`).prop("checked", allChecked);
                updateGlobalSelectAll();
            });

            updateGlobalSelectAll();
        },
        error: function () {
            $("#permissionsContainer").html("<p class='text-danger'>⚠️ Failed to load permissions.</p>");
        }
    });
}


// Global select all
$("#selectAllGlobal").on("change", function () {
    let isChecked = $(this).is(":checked");
    $(".perm-checkbox, .select-all").prop("checked", isChecked);
});

// Update global checkbox based on child states
function updateGlobalSelectAll() {
    let allChecked = $(".perm-checkbox").length > 0 &&
        $(".perm-checkbox:checked").length === $(".perm-checkbox").length;
    $("#selectAllGlobal").prop("checked", allChecked);
}

// Save
function savePermissions() {
    if (!currentRoleId) {
        Swal.fire("⚠️ Oops!", "Please select a role first!", "warning");
        return;
    }

    let permissions = [];
    $(".perm-checkbox").each(function () {
        permissions.push({
            roleID: currentRoleId,
            menuModuleID: $(this).data("menuid"),
            actionID: $(this).data("actionid"),
            hasPermission: $(this).is(":checked")
        });
    });

    $.ajax({
        url: `/RolePermission/SavePermissions?roleId=${currentRoleId}`,
        type: "POST",
        contentType: "application/json",
        data: JSON.stringify(permissions),
        success: function (res) {
            if (res > 0) {
                Swal.fire({
                    icon: "success",
                    title: "✅ Saved!",
                    text: "Permissions saved successfully!",
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    $("#permissionModal").modal("hide");
                    location.reload();
                });
            } else {
                Swal.fire("❌ Error", "Failed to save permissions.", "error");
            }
        }
    });
}

// Deactivate all permissions of a role
function deactivatePermission(roleId) {
    Swal.fire({
        title: "Are you sure?",
        text: "This will deactivate all permissions for the selected role.",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#d33",
        cancelButtonColor: "#6c757d",
        confirmButtonText: "Yes, deactivate!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: `/RolePermission/DeleteRolePermission?roleId=${roleId}`,
                type: "DELETE",
                success: function (res) {
                    if (res > 0) {
                        Swal.fire({
                            icon: "success",
                            title: "✅ Deactivated!",
                            text: "All permissions have been removed.",
                            timer: 2000,
                            showConfirmButton: false
                        }).then(() => location.reload());
                    } else {
                        Swal.fire("❌ Error", "Failed to deactivate permissions.", "error");
                    }
                },
                error: function () {
                    Swal.fire("⚠️ Error", "Server error calling deactivate API.", "error");
                }
            });
        }
    });
}
