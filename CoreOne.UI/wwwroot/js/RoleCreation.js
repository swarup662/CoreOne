$("#btnSaveRole").click(function () {
    // Build payload from form
    var payload = {
        RoleID: $("#RoleID").val() || 0,
        RoleName: $("#RoleName").val().trim(),
        RoleDescription: $("#RoleDescription").val().trim(),
        DisplayOn: $("#DisplayOn").val(),     // <-- Added
        ActiveFlag: $("#ActiveFlag").val()
    };

    // Clear old errors
    $("#RoleName, #RoleDescription").removeClass("is-invalid is-valid");
    $("#RoleName").next(".text-danger").text('');
    $("#RoleDescription").next(".text-danger").text('');
    validateField("RoleName")
    validateField("RoleDescription")

    // AJAX call to save
    $.ajax({
        url: '/RoleCreation/SaveRole',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (resp) {
            if (resp.success) {
                $("#roleModal").modal('hide');
                Swal.fire({
                    icon: 'success',
                    title: 'Success!',
                    text: 'Role saved successfully.',
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    location.reload();
                });
            } else {
                if (resp.message == 'exist') {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error!',
                        text: 'Role name already exist.',
                        timer: 3000,
                        showConfirmButton: true
                    });
                }

            }
        },
        error: function () {
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: 'Something went wrong while saving.',
                timer: 3000,
                showConfirmButton: true
            });
        }
    });
});

function validateField(fieldId) {
    let payload = {};
    payload[fieldId] = $("#" + fieldId).val().trim();

    $.ajax({
        url: '/RoleCreation/ValidateField', // <-- New endpoint just for field validation
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (resp) {
            let $input = $("#" + fieldId);
            let $errorSpan = $input.next(".text-danger");

            $input.removeClass("is-invalid is-valid");
            $errorSpan.text('');

            if (resp.errors && resp.errors[fieldId]) {
                $input.addClass("is-invalid");
                $errorSpan.text(resp.errors[fieldId][0]);
            } else {
                $input.addClass("is-valid");
            }
        }
    });
}

// Attach onchange (or input for real-time typing)
$("#RoleName, #RoleDescription").on("change input", function () {
    validateField(this.id);
});




function loadRole(roleId) {
    // Update modal header and aria-labelledby for edit
    $('#roleModalLabel').text('Update Role :: [Edit]');
    $('#roleModal').attr('aria-labelledby', 'roleModalLabel');

    $.ajax({
        url: '/RoleCreation/GetRoleById', // UI controller action
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(roleId),
        success: function (role) {
            $('#RoleID').val(role.roleID);
            $('#RoleName').val(role.roleName);
            $('#RoleDescription').val(role.roleDescription);
            $('#roleModal').modal('show');
        },
        error: function () {
            alert('Could not load role details!');
            $('#roleModal').modal('hide');
        }
    });
}
function deleteRole(roleId) {
    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete role`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/RoleCreation/DeleteRole',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(roleId),
                success: function (resp) {
                    if (resp.success) {
                        Swal.fire(
                            'Deleted!',
                            `Role has been deleted.`,
                            'success'
                        ).then(() => {
                            location.reload(); // reload table
                        });
                    } else {
                        Swal.fire('Error!', 'Could not delete role.', 'error');
                    }
                },
                error: function () {
                    Swal.fire('Error!', 'Something went wrong.', 'error');
                }
            });
        }
    });
}