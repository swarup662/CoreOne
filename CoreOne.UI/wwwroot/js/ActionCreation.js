function loadRole(actionId) {
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
