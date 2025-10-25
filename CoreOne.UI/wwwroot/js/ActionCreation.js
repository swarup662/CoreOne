function loadAction(actionId) {
    // Update modal header for edit
    $('#actionModalLabel').text('Update Action :: [Edit]');
    $('#ActionModal').attr('aria-labelledby', 'actionModalLabel');

    console.log("📌 Sending Action ID:", actionId);

    const requestData = JSON.stringify(actionId);
    console.log("📤 JSON Request Data:", requestData); // ✅ Log JSON.stringify(actionId)

    $.ajax({
        url: '/ActionCreation/GetActionById',
        type: 'POST',
        contentType: 'application/json',
        data: requestData,
        success: function (action) {

            console.log("✅ Server Response:", action);

            $('#ActionID').val(action.actionID);
            $('#ActionName').val(action.actionName);
            $('#Description').val(action.description);

            $('#ActionModal').modal('show');
        },
        error: function (xhr) {
            console.error("❌ AJAX Error:", xhr.responseText);
            alert('Could not load action details!');
            $('#ActionModal').modal('hide');
        }
    });
}







$("#btnSaveAction").click(async function () {
    // Clear old errors
    $("#ActionID, #ActionName, #Description").removeClass("is-invalid is-valid");
    $("#ActionName, #Description").next(".text-danger").text('');

    let hasError = false;

    // Check empty fields
    if (!$("#ActionName").val().trim()) {
        $("#ActionName").addClass("is-invalid");
        $("#ActionName").next(".text-danger").text("Action name is required.");
        hasError = true;
    }

    if (!$("#Description").val().trim()) {
        $("#Description").addClass("is-invalid");
        $("#Description").next(".text-danger").text("Description is required.");
        hasError = true;
    }

    if (hasError) return;

    // Server-side validation
    try {
        const [isNameValid, isDescValid] = await Promise.all([
            validateField("ActionName"),
            validateField("Description")
        ]);

        if (!isNameValid || !isDescValid) return;
    } catch {
        Swal.fire({
            icon: 'error',
            title: 'Error!',
            text: 'Something went wrong during validation.',
            timer: 3000,
            showConfirmButton: true
        });
        return;
    }

    const payload = {
        ActionID: $("#ActionID").val() || 0,
        ActionName: $("#ActionName").val().trim(),
        Description: $("#Description").val().trim()
    };

    // Save / Update AJAX
    $.ajax({
        url: '/ActionCreation/SaveAction',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (resp) {
            let message = '';
            let icon = '';

            if (resp.success) {
                icon = 'success';
                message = 'Action saved successfully.';
            } else if (resp.message === 'exist') {
                icon = 'error';
                message = 'Action name already exists.';
            } else {
                icon = 'error';
                message = 'Something went wrong while saving.';
            }

            Swal.fire({
                icon: icon,
                title: icon === 'success' ? 'Success!' : 'Error!',
                text: message,
                timer: 3000,
                showConfirmButton: true
            }).then(() => {
                $("#ActionModal").modal('hide');
                if (icon === 'success') location.reload();
            });
        },
        error: function () {
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: 'Something went wrong while saving.',
                timer: 3000,
                showConfirmButton: true
            }).then(() => $("#ActionModal").modal('hide'));
        }
    });
});

function validateField(fieldId) {
    return new Promise((resolve, reject) => {
        let payload = {};
        payload[fieldId] = $("#" + fieldId).val().trim();

        $.ajax({
            url: '/ActionCreation/ValidateField',
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
                    resolve(false);
                } else {
                    $input.addClass("is-valid");
                    resolve(true);
                }
            },
            error: function () {
                reject(); // Reject promise if AJAX fails
            }
        });
    });
}

// Real-time validation
$("#ActionName, #Description").on("input change", function () {
    validateField(this.id).catch(() => { });
});



function deleteAction(actionId) {
    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete Action`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/ActionCreation/DeleteAction',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(actionId),
                success: function (resp) {
                    if (resp.success) {
                        Swal.fire(
                            'Deleted!',
                            `Action has been deleted.`,
                            'success'
                        ).then(() => {
                            location.reload(); // reload table
                        });
                    } else {
                        Swal.fire('Error!', 'Could not delete Action.', 'error');
                    }
                },
                error: function () {
                    Swal.fire('Error!', 'Something went wrong.', 'error');
                }
            });
        }
    });
}