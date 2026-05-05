/**
 * ValidationHelper - Hỗ trợ hiển thị lỗi validation chuyên nghiệp cho Admin
 */
const ValidationHelper = {
    /**
     * Hiển thị lỗi từ Backend (ModelState) lên các ô input tương ứng
     * @param {jQuery} $form - Form chứa các input
     * @param {Object} errors - Dictionary lỗi từ Backend (Key: Tên trường, Value: Mảng thông báo)
     */
    showErrors: function ($form, errors) {
        this.clearErrors($form);
        
        if (!errors) return;

        Object.keys(errors).forEach(key => {
            // Chuyển Key từ "Items[0].SKU" thành "SKU" hoặc tìm theo ID tương ứng
            // Đối với các mảng (như Items[0]), ta thường tìm theo cấu trúc linh hoạt
            let fieldName = key.split('.').pop(); 
            let $input = $form.find(`[name="${key}"], [id="${key}"], [id="${fieldName.toLowerCase()}Input"], [id="${fieldName.charAt(0).toLowerCase() + fieldName.slice(1)}Input"]`);
            
            if ($input.length > 0) {
                this.showFieldError($input, errors[key][0]);
            }
        });
    },

    /**
     * Hiển thị lỗi cho một ô input cụ thể
     * @param {jQuery} $input - Ô input bị lỗi
     * @param {string} message - Thông báo lỗi
     */
    showFieldError: function ($input, message) {
        $input.addClass('border-red-500 bg-red-50/30').removeClass('border-slate-200 focus:border-blue-600');
        
        // Tìm hoặc tạo thẻ chứa lỗi
        let $errorSpan = $input.parent().find('.invalid-feedback');
        if ($errorSpan.length === 0) {
            $errorSpan = $('<span class="invalid-feedback text-red-500 text-[10px] font-bold mt-1 block animate-pulse"></span>');
            $input.after($errorSpan);
        }
        
        $errorSpan.text(message).removeClass('hidden');
    },

    /**
     * Xóa toàn bộ lỗi trên form
     * @param {jQuery} $form - Form cần xóa lỗi
     */
    clearErrors: function ($form) {
        $form.find('.border-red-500').removeClass('border-red-500 bg-red-50/30').addClass('border-slate-200');
        $form.find('.invalid-feedback').addClass('hidden').text('');
    }
};

// Gắn vào window để gọi từ mọi file JS khác
window.ValidationHelper = ValidationHelper;
