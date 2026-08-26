$('#avatarInput').on('change', function () {
    const file = this.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('avatar', file);

    $.ajax({
        url: '/Admin/Profile/UploadAvatar',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (res) {
            if (res.success) {
                if (res.avatarUrl) {
                    $('#avatarPreview').html(`<img src="${res.avatarUrl}" alt="Avatar" />`);
                }
                toastr.success(res.message);
                if (typeof loadAdminHeaderProfile === 'function') loadAdminHeaderProfile();
            } else {
                Swal.fire('Lỗi', res.message, 'error');
            }
        },
        error: function () {
            Swal.fire('Lỗi', 'Tải ảnh thất bại', 'error');
        }
    });
});

$('#profileForm').on('submit', function (e) {
    e.preventDefault();
    const $form = $(this);
    if (!ValidationHelper.validateForm($form)) return;

    $.ajax({
        url: '/Admin/Profile/UpdateProfile',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            fullName: $('#fullName').val(),
            phoneNumber: $('#phoneNumber').val(),
            department: $('#department').val()
        }),
        success: function (res) {
            if (res.success) {
                $('#displayFullName').text($('#fullName').val());
                $('#displayDepartment').text($('#department').val() || 'Chưa có phòng ban');
                toastr.success(res.message);
                if (typeof loadAdminHeaderProfile === 'function') loadAdminHeaderProfile();
            } else {
                toastr.error(res.message || 'Không thể cập nhật hồ sơ');
            }
        }
    });
});

$('#passwordForm').on('submit', function (e) {
    e.preventDefault();
    const $form = $(this);
    if (!ValidationHelper.validateForm($form)) return;

    const newPwd = $('#newPassword').val();
    const confirmPwd = $('#confirmPassword').val();

    if (newPwd !== confirmPwd) {
        ValidationHelper.showFieldError($('#confirmPassword'), 'Xác nhận mật khẩu không khớp');
        return;
    }

    $.ajax({
        url: '/Admin/Profile/ChangePassword',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            currentPassword: $('#currentPassword').val(),
            newPassword: newPwd,
            confirmPassword: confirmPwd
        }),
        success: function (res) {
            if (res.success) {
                toastr.success(res.message);
                $('#passwordForm')[0].reset();
            } else {
                ValidationHelper.showFieldError($('#currentPassword'), res.message || 'Không thể đổi mật khẩu');
            }
        }
    });
});
