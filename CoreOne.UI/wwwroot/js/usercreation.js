
$(document).ready(function () {
    $('#showFile').hide()

});

function closeUserCreationPage() {
    window.location.href = '/UserCreation/Index';
}


$("#btnSaveUser").click(async function () {


    const ModeUserId = parseInt($("#UserID").val()) || 0;
    var isPasswordValid = false;

    // Clear validation classes & messages
    $("#UserName, #Email, #PhoneNumber, #RoleID, #GenderID, #MailTypeID, #PasswordHash", )
        .removeClass("is-invalid is-valid");
    $(".text-danger").text('');

   
    // Validate all required fields before saving
    const isUserNameValid = await validateField("UserName");

    if (ModeUserId > 0) {
        // Existing user → edit mode
        isPasswordValid = await validatePasswordField("PasswordHash");
    } else {
        // New user → create mode
        isPasswordValid = await validateField("PasswordHash");
        

    }
    
    const isEmailValid = await validateField("Email");
    const isPhoneValid = await validateField("PhoneNumber");
    const isRoleValid = await validateField("RoleID");
    const isGenderValid = await validateField("GenderID");
    const isMailTypeValid = await validateField("MailTypeID");
    var relativePath = null;
    var filename = null
    // Stop if any validation fails
    if (!isUserNameValid || !isPasswordValid || !isEmailValid || !isPhoneValid ||
        !isRoleValid || !isGenderValid || !isMailTypeValid ) {
     
        return; // 🔥 Stop here — don’t run the save AJAX
    }


    //-----------file uploadation code-------------------//
    const userId = $("#UserID").val(); // get the value

    if (userId && !isNaN(userId) && Number(userId) > 0 && tempBlobCommonId ==null) {
       // console.log("✅ Valid UserID);
    } else {
        console.log("❌ Invalid UserID ");

        const result = await saveFile();
        var IsfileUploaded = false;
        let $fileinput = $("#" + "PhotoPath");
        let $fileerrorSpan = $fileinput.next(".text-danger");
        $fileinput.removeClass("is-invalid is-valid");
        $fileerrorSpan.text('');

        if (result.success) {
            IsfileUploaded = true
            $fileinput.addClass("is-valid");
            $("#PhotoPath").text("");
            relativePath = result.relativePath;
            filename = result.fileName;
        }
        else {

            $fileinput.addClass("is-invalid");

            $("#PhotoPath").text("Please upload a photo");


        }
        // Stop if any validation fails
        if (!IsfileUploaded) {

            return; // 🔥 Stop here — don’t run the save AJAX
        }
        //-------------------------------------------------------//
    }

    //Build Payload
    var payload = {
        UserID: $("#UserID").val() || 0,
        UserName: $("#UserName").val().trim(),
        PasswordHash: $("#PasswordHash").val().trim(),
        Email: $("#Email").val().trim(),
        PhoneNumber: $("#PhoneNumber").val().trim(),
        RoleID: $("#RoleID").val(),
        GenderID: $("#GenderID").val(),
        MailTypeID: $("#MailTypeID").val(),
        PhotoPath: relativePath,
        PhotoName: filename
    };
    // --- All validations passed, proceed to save ---
    $.ajax({
        url: '/UserCreation/SaveUser',
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
            url: '/UserCreation/ValidateField',
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
function validatePasswordField(fieldId) {
    return new Promise((resolve) => {
        let payload = {};
        payload[fieldId] = $("#" + fieldId).val()?.trim() || "";

        $.ajax({
            url: '/UserCreation/ValidatePasswordField',
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
$("#PasswordHash")
    .on("change input", function () {
        const userId = parseInt($("#UserID").val()) || 0;

        if (userId > 0) {
            // Existing user → edit mode
            validatePasswordField(this.id);
        } else {
            // New user → create mode
          
            validateField(this.id);

        }
    });

function editUser(id) {
    // Update modal header and aria-labelledby for edit
    $('#addUserModalLabel').text('Edit User');
    $('#addUserModal').attr('aria-labelledby', 'userModalLabel');

    $.ajax({
        url: '/UserCreation/GetUserById',// UI controller action
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

            //-----------file uploadation code-------------------//


            $('#showFile').show();
            if (user.photoPath === "" || user.photoPath === null || user.photoPath === undefined) {
                $('#showFile').hide();

            }
            else {
                loadProfilePhoto(user.photoPath);
                prepareSavedFilePreview(user.photoPath);
            }

            //------------------------------------------------------//

            $('#addUserModal').modal('show');
        },
        error: function () {
            alert('Could not load user details!');
            $('#addUserModal').modal('hide');
        }
    });
}
function viewUser(id) {
    // Update modal header and aria-labelledby for edit
    $('#addUserModalLabel').text('Edit User');
    $('#addUserModal').attr('aria-labelledby', 'userModalLabel');

    $.ajax({
        url: '/UserCreation/GetUserById',// UI controller action
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
            $("#btnSaveUser").remove();
            //-----------file uploadation code-------------------//


            $('#showFile').show();
            if (user.photoPath === "" || user.photoPath === null || user.photoPath === undefined) {
                $('#showFile').hide();

            }
            else {
                loadProfilePhoto(user.photoPath);
                prepareSavedFilePreview(user.photoPath);
            }

            //------------------------------------------------------//

            $('#addUserModal').modal('show');
        },
        error: function () {
            alert('Could not load user details!');
            $('#addUserModal').modal('hide');
        }
    });
}

function deactivateUser(id) {
    Swal.fire({
        title: 'Are you sure?',
        text: "Do you really want to deactivate this user?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, deactivate',
        cancelButtonText: 'Cancel',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/UserCreation/DeactivateUser',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(id),
                success: function (res) {
                    if (res.success) {
                        Swal.fire({
                            title: 'Deactivated!',
                            text: 'User has been deactivated.',
                            icon: 'success',
                            timer: 1500,
                            showConfirmButton: false
                        }).then(() => location.reload());
                    } else {
                        Swal.fire({
                            title: 'Failed!',
                            text: 'Failed to deactivate user.',
                            icon: 'error'
                        });
                    }
                }
            });
        }
    });
}
function activateUser(id) {
    Swal.fire({
        title: 'Are you sure?',
        text: "Do you really want to activate this user?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, Activate',
        cancelButtonText: 'Cancel',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/UserCreation/ActivateUser',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(id),
                success: function (res) {
                    if (res.success) {
                        Swal.fire({
                            title: 'Activated!',
                            text: 'User has been Activated.',
                            icon: 'success',
                            timer: 1500,
                            showConfirmButton: false
                        }).then(() => location.reload());
                    } else {
                        Swal.fire({
                            title: 'Failed!',
                            text: 'Failed to activate user.',
                            icon: 'error'
                        });
                    }
                }
            });
        }
    });
}


document.getElementById("uploadFileInput").addEventListener("change", async function () {
    if ($("#showFile").length > 0) {
        $("#showFile").hide();
    }

});

async function loadProfilePhoto(filePath) {
    const img = document.getElementById("profilePreview");
    const defaultSrc = "/images/default-user.png";

    // Handle invalid, null, or empty path
    if (!filePath || filePath.trim() === "" || filePath === "null" || filePath === "undefined") {
        img.src = defaultSrc;
        return;
    }

    try {
        const resp = await fetch(`/api/FileUpload/viewSaved?filePath=${encodeURIComponent(filePath)}`);
        if (!resp.ok) throw new Error("Photo not found");

        const blob = await resp.blob();
        const blobUrl = URL.createObjectURL(blob);

        img.src = blobUrl;
    } catch (err) {
        console.error("Error loading profile photo:", err);
        img.src = defaultSrc;
    }
}







let currentUserId = 0;
let currentRoleId = 0;
let isAddMode = false;
function openExtraPermissionModal(userId,roleId) {
    isAddMode = false;
    currentRoleId = roleId;
    currentUserId = userId
    $("#roleDropdown option").each(function () {
        if ($(this).val() != roleId) {
            $(this).remove(); // remove all options except the selected one
        }
    });
    $("#roleDropdown").val(roleId);// ensure selected value
    $("#permissionsContainer").html("<p class='text-muted'>Loading...</p>");
    $("#permissionModal").modal("show");
    $("#selectAllGlobal").prop("checked", false);

    fetchPermissions(userId, roleId);
}

function fetchPermissions(userId, roleId, ) {
    $("#permissionsContainer").html("<p class='text-muted'>Loading...</p>");

    $.ajax({
        url: "/UserCreation/GetRoleAndExtraPermissions",
        type: "GET",
        data: { userId: userId ,roleId: roleId },
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

function updateGlobalSelectAll() {
    let allChecked = $(".perm-checkbox").length > 0 &&
        $(".perm-checkbox:checked").length === $(".perm-checkbox").length;
    $("#selectAllGlobal").prop("checked", allChecked);
}


async function extraPermission(UserID, RoleID) {
    openExtraPermissionModal(UserID,RoleID)

}



// Save
function saveExtraPermissions() {
     currentRoleId = $("#roleDropdown").val();
    if (!currentRoleId) {
        Swal.fire("⚠️ Oops!", "Role is showing in  modal. Please contact Admin!", "warning");
        return;
    }

    let permissions = [];

    // Only process checkboxes with the 'extra' class
    $(".perm-checkbox.extra").each(function () {
        permissions.push({
            roleID: currentRoleId,
             userID: currentUserId,
            menuModuleID: $(this).data("menuid"),
            actionID: $(this).data("actionid"),
            hasPermission: $(this).is(":checked")
         
        });
    });

    if (permissions.length === 0) {
        Swal.fire("ℹ️ Nothing to save", "No changes found to save.", "info");
        return;
    }

    $.ajax({
        url: `/UserCreation/SaveExtraPermissions`,
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
$(document).on("change", ".perm-checkbox", function () {
    if ($(this).is(":checked")) {
        $(this).attr("checked", "checked");
    } else {
        $(this).removeAttr("checked");
    }
});



    $(document).ready(function () {
        const $input = $('#searchUser');
    const $list = $('#userSuggestions');

    $input.on('keyup', function () {
            const term = $(this).val().trim();

    if (term.length < 2) {
        $list.hide();
    return;
            }

    $.ajax({
        url: '/UserCreation/SearchUserName',
    type: 'GET',
    data: {term: term },
    success: function (data) {
        $list.empty();
                    if (data && data.length > 0) {
        data.forEach(item => {
            const li = $('<li>')
                .addClass('list-group-item list-group-item-action')
                .text(item.UserName)
                .attr('data-id', item.UserID)
                .on('click', function () {
                    $input.val($(this).text());
                    $('#notifyUserId').val($(this).data('id')); // sets hidden userId field
                    $list.hide();
                });
            $list.append(li);
        });
    $list.show();
                    } else {
        $list.hide();
                    }
                },
    error: function () {
        $list.hide();
                }
            });
        });

    // Hide dropdown when clicking outside
    $(document).on('click', function (e) {
            if (!$(e.target).closest('#searchUser, #userSuggestions').length) {
        $list.hide();
            }
        });
    });
