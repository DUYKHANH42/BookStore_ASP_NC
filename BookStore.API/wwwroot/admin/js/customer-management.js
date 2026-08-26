function openCustomerModal(id) {
    const modal = document.getElementById('customerModal');
    const modalBody = document.getElementById('customerModalBody');

    modalBody.innerHTML = `
        <div class="flex flex-col items-center justify-center py-20">
            <div class="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mb-4"></div>
            <p class="font-bold text-slate-400">Đang tải dữ liệu khách hàng...</p>
        </div>
    `;
    modal.classList.add('active');

    $.get('/Admin/Customer/GetCustomerDetails/' + id, function (data) {
        modalBody.innerHTML = data;
    }).fail(function() {
        modalBody.innerHTML = '<div class="p-10 text-center text-rose-500 font-bold">Lỗi khi tải dữ liệu!</div>';
    });
}

function closeCustomerModal() {
    const modal = document.getElementById('customerModal');
    modal.classList.remove('active');
}

$(document).on('submit', '#resetPasswordForm', function (e) {
    e.preventDefault();
    const $form = $(this);
    const id = $('#customerId').val();
    const newPassword = $('#newPassword').val();
    const btn = $(this).find('button[type="submit"]');

    if (!ValidationHelper.validateForm($form)) return;
    if (newPassword.length < 6) {
        ValidationHelper.showFieldError($('#newPassword'), 'Mật khẩu phải ít nhất 6 ký tự');
        return;
    }

    btn.prop('disabled', true).html('Đang xử lý...');

    $.post('/Admin/Customer/ResetPassword', { id: id, newPassword: newPassword }, function (res) {
        if (res.success) {
            toastr.success(res.message);
            closeCustomerModal();
        } else {
            ValidationHelper.showFieldError($('#newPassword'), res.message || 'Không thể cập nhật mật khẩu');
            btn.prop('disabled', false).html('Cập nhật mật khẩu');
        }
    });
});

$(document).on('click', '.btn-toggle-status', function () {
    const id = $(this).data('id');
    const isActive = $(this).data('active');
    const message = isActive ? "Bạn có chắc muốn KHÓA tài khoản này?" : "Bạn có chắc muốn MỞ KHÓA tài khoản này?";

    LumenAlert.confirm({
        title: 'Xác nhận?',
        text: message,
        confirmButtonText: 'Đồng ý',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        if (!result.isConfirmed) return;

        $.post('/Admin/Customer/UpdateStatus', { id: id, isActive: !isActive }, function (res) {
            if (res.success) {
                toastr.success(res.message);
                setTimeout(() => location.reload(), 800);
            } else {
                toastr.error(res.message || 'Không thể cập nhật trạng thái');
            }
        });
    });
});

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeCustomerModal();
});
