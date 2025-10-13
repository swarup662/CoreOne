$("#btnSaveUser").click(async function () {
    // Build payload
    var payload = {
        UserID: $("#UserID").val() || 0,
        UserName: $("#UserName").val().trim(),
        Email: $("#Email").val().trim(),
        PhoneNumber: $("#PhoneNumber").val().trim(),
        RoleID: $("#RoleID").val(),
        GenderID: $("#GenderID").val(),
        MailTypeID: $("#MailTypeID").val()
    };
    const result = await saveFile();
    var IsfileUploaded =false;
    if (result.success) {
        IsfileUploaded = true
    } 
    // Clear validation classes & messages
    $("#UserName, #Email, #PhoneNumber, #RoleID, #GenderID, #MailTypeID,#PhotoPath ", )
        .removeClass("is-invalid is-valid");
    $(".text-danger").text('');

    // Validate all required fields before saving
    const isUserNameValid = await validateField("UserName");
    const isEmailValid = await validateField("Email");
    const isPhoneValid = await validateField("PhoneNumber");
    const isRoleValid = await validateField("RoleID");
    const isGenderValid = await validateField("GenderID");
    const isMailTypeValid = await validateField("MailTypeID");

    // Stop if any validation fails
    if (!isUserNameValid || !isEmailValid || !isPhoneValid ||
        !isRoleValid || !isGenderValid || !isMailTypeValid || IsfileUploaded) {
     
        return; // 🔥 Stop here — don’t run the save AJAX
    }

    // --- All validations passed, proceed to save ---
    $.ajax({
        url: '/UserCreationUI/SaveUser',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (resp) {
            if (resp.success) {
                $("#addUserModal").modal('hide');
                Swal.fire({
                    icon: 'success',
                    title: 'Success!',
                    text: 'User saved successfully.',
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => location.reload());
            } else if (resp.message === 'exist') {
                Swal.fire({
                    icon: 'error',
                    title: 'Duplicate Email',
                    text: 'User email already exists.',
                    timer: 3000,
                    showConfirmButton: true
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error!',
                    text: resp.message || 'Unable to save user.',
                    timer: 3000,
                    showConfirmButton: true
                });
            }
        },
        error: function () {
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: 'Something went wrong while saving user.',
                timer: 3000,
                showConfirmButton: true
            });
        }
    });
});


// ✅ Field validation with return true/false
function validateField(fieldId) {
    return new Promise((resolve) => {
        let payload = {};
        payload[fieldId] = $("#" + fieldId).val()?.trim() || "";

        $.ajax({
            url: '/UserCreationUI/ValidateField',
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
                    resolve(false); // ❌ Validation failed
                } else {
                    $input.addClass("is-valid");
                    resolve(true); // ✅ Validation passed
                }
            },
            error: function () {
                resolve(false); // If validation API fails, treat as invalid
            }
        });
    });
}


// ✅ Real-time validation (on typing / dropdown change)
$("#UserName, #Email, #PhoneNumber, #RoleID, #GenderID, #MailTypeID")
    .on("change input", function () {
        validateField(this.id);
    });

function editUser(id) {
    // Update modal header and aria-labelledby for edit
    $('#userModalLabel').text('Edit User');
    $('#addUserModal').attr('aria-labelledby', 'userModalLabel');

    $.ajax({
        url: '/UserCreationUI/GetUserById',// UI controller action
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(id),
        success: function (user) {
            $('#UserID').val(user.userID);
            $('#UserName').val(user.userName);
            $('#Email').val(user.email);
            $('#PhoneNumber').val(user.phoneNumber);
            $('#RoleID').val(user.roleID);
            $('#GenderID').val(user.genderID);
            $('#MailTypeID').val(user.mailTypeID);

            $('#addUserModal').modal('show');
        },
        error: function () {
            alert('Could not load user details!');
            $('#addUserModal').modal('hide');
        }
    });
}

function deleteUser(id) {
    if (confirm("Are you sure you want to Deactivate this user?")) {
        $.ajax({
            url: '/UserCreationUI/DeleteUser',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(id),
            success: function (res) {
                if (res.success) location.reload();
                else alert("Failed to delete user");
            }
        });
    }
}