
$(document).ready(function () {
    $('#showFile').hide()

});

function closeUserCreationPage() {
    window.location.href = '/UserCreationUI/Index';
}


$("#btnSaveUser").click(async function () {
  


    // Clear validation classes & messages
    $("#UserName, #Email, #PhoneNumber, #RoleID, #GenderID, #MailTypeID", )
        .removeClass("is-invalid is-valid");
    $(".text-danger").text('');

    // Validate all required fields before saving
    const isUserNameValid = await validateField("UserName");
    const isEmailValid = await validateField("Email");
    const isPhoneValid = await validateField("PhoneNumber");
    const isRoleValid = await validateField("RoleID");
    const isGenderValid = await validateField("GenderID");
    const isMailTypeValid = await validateField("MailTypeID");
    var relativePath = null;
    var filename = null
    // Stop if any validation fails
    if (!isUserNameValid || !isEmailValid || !isPhoneValid ||
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
    $('#addUserModalLabel').text('Edit User');
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








function openExtraPermissionModal(userId,roleId) {
    isAddMode = false;
    currentRoleId = roleId;

    $("#roleDropdown").val(roleId);
    $("#permissionsContainer").html("<p class='text-muted'>Loading...</p>");
    $("#permissionModal").modal("show");
    $("#selectAllGlobal").prop("checked", false);

    fetchPermissions(userId, roleId);
}

function fetchPermissions(userId, roleId, ) {
    $("#permissionsContainer").html("<p class='text-muted'>Loading...</p>");

    $.ajax({
        url: "/UserCreationUI/GetRoleAndExtraPermissions",
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