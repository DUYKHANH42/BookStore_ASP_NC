let pendingStaffPayload = null;

function selectStaffRole(btn) {
    const role = btn.getAttribute('data-role');
    document.getElementById('staffRole').value = role;
    document.querySelectorAll('.role-picker-card').forEach(card => {
        card.classList.remove('role-picker-active');
    });
    btn.classList.add('role-picker-active');
}

function openCreateStaffModal() {
    document.getElementById('createStaffForm').reset();
    ValidationHelper.clearErrors($('#createStaffForm'));
    document.getElementById('staffRole').value = 'Employee';
    document.querySelectorAll('.role-picker-card').forEach(card => {
        card.classList.remove('role-picker-active');
        if (card.getAttribute('data-role') === 'Employee') {
            card.classList.add('role-picker-active');
        }
    });
    document.getElementById('createStaffModal').classList.add('active');
}

function closeCreateStaffModal() {
    document.getElementById('createStaffModal').classList.remove('active');
}

function openConfirmIdentityModal() {
    document.getElementById('adminConfirmPassword').value = '';
    ValidationHelper.clearErrors($('#confirmIdentityForm'));
    document.getElementById('confirmIdentityModal').classList.add('active');
}

function closeConfirmIdentityModal() {
    document.getElementById('confirmIdentityModal').classList.remove('active');
}

function backToCreateForm() {
    closeConfirmIdentityModal();
    document.getElementById('createStaffModal').classList.add('active');
}

$('#createStaffForm').on('submit', function (e) {
    e.preventDefault();

    const $form = $(this);
    if (!ValidationHelper.validateForm($form)) return;

    const password = $('#staffPassword').val();
    const confirmPassword = $('#staffConfirmPassword').val();

    if (password !== confirmPassword) {
        ValidationHelper.showFieldError($('#staffConfirmPassword'), 'Mật khẩu xác nhận không khớp');
        return;
    }

    if (password.length < 8) {
        ValidationHelper.showFieldError($('#staffPassword'), 'Mật khẩu phải ít nhất 8 ký tự');
        return;
    }

    pendingStaffPayload = {
        fullName: $('#staffFullName').val().trim(),
        email: $('#staffEmail').val().trim(),
        phoneNumber: $('#staffPhone').val() || null,
        department: $('#staffDepartment').val().trim(),
        role: $('#staffRole').val(),
        password: password,
        confirmPassword: confirmPassword
    };

    const isAdmin = pendingStaffPayload.role === 'Admin';
    const roleLabel = isAdmin ? 'Quản trị viên (Admin)' : 'Nhân viên (Employee)';
    $('#confirmStaffSummary').html(
        isAdmin
            ? `<strong class="text-rose-700">Tạo Admin mới</strong> — ${pendingStaffPayload.fullName} (${pendingStaffPayload.email}) sẽ có <strong>toàn quyền</strong> hệ thống, bao gồm tạo thêm Admin khác. Nhập mật khẩu admin hiện tại để xác nhận.`
            : `Tạo tài khoản ${roleLabel}: ${pendingStaffPayload.fullName} (${pendingStaffPayload.email}). Nhập mật khẩu admin hiện tại để xác nhận.`
    );

    closeCreateStaffModal();
    openConfirmIdentityModal();
});

$('#confirmIdentityForm').on('submit', function (e) {
    e.preventDefault();
    if (!pendingStaffPayload) return;

    const $form = $(this);
    if (!ValidationHelper.validateForm($form)) return;

    const btn = $(this).find('button[type="submit"]');
    const orig = btn.html();
    btn.prop('disabled', true).html('Đang xử lý...');

    const payload = {
        ...pendingStaffPayload,
        adminConfirmPassword: $('#adminConfirmPassword').val()
    };

    $.ajax({
        url: '/Admin/Staff/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (res) {
            btn.prop('disabled', false).html(orig);
            if (res.success) {
                toastr.success(res.message);
                setTimeout(() => location.reload(), 800);
            } else {
                ValidationHelper.showFieldError($('#adminConfirmPassword'), res.message || 'Mật khẩu xác nhận không đúng');
            }
        },
        error: function (xhr) {
            btn.prop('disabled', false).html(orig);
            const msg = xhr.responseJSON?.message || 'Lỗi hệ thống';
            ValidationHelper.showFieldError($('#adminConfirmPassword'), msg);
        }
    });
});

$(document).on('click', '.btn-toggle-staff-status', function () {
    const id = $(this).data('id');
    const isActive = $(this).data('active');
    const message = isActive
        ? 'Bạn có chắc muốn KHÓA tài khoản nhân sự này?'
        : 'Bạn có chắc muốn MỞ KHÓA tài khoản nhân sự này?';

    LumenAlert.confirm({
        title: 'Xác nhận?',
        text: message,
        confirmButtonText: 'Đồng ý',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        if (result.isConfirmed) {
            $.post('/Admin/Staff/UpdateStatus', { id: id, isActive: !isActive }, function (res) {
                if (res.success) {
                    toastr.success(res.message);
                    setTimeout(() => location.reload(), 800);
                } else {
                    LumenAlert.error('Lỗi', res.message);
                }
            });
        }
    });
});

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        closeCreateStaffModal();
        closeConfirmIdentityModal();
    }
});
