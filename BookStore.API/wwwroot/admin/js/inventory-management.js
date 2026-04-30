$(document).ready(function () {
    let skuTimer;

    // 1. Xử lý gõ SKU
    $('#skuInput').on('input', function () {
        const sku = $(this).val();
        clearTimeout(skuTimer);

        if (sku.length < 3) {
            hideNewProductSection();
            return;
        }

        skuTimer = setTimeout(() => {
            checkSKU(sku);
        }, 500);
    });

    function checkSKU(sku) {
        const icon = $('.sku-status-icon');
        icon.html('<span class="material-symbols-outlined animate-spin text-slate-300">refresh</span>');

        $.get('/Admin/Inventory/CheckSKU', { sku: sku }, function (res) {
            if (res.exists) {
                icon.html('<span class="material-symbols-outlined text-emerald-500">check_circle</span>');
                $('#productInfoAlert').show().find('.p-name').text(res.name);

                // Điền thông tin cũ vào form để admin có thể cập nhật nếu muốn
                $('#nameInput').val(res.name);
                $('#brandInput').val(res.brand);
                $('#sellingPriceInput').val(res.price);
                $('#descriptionInput').val(res.description);
                $('#categorySelect').val(res.categoryId).trigger('change', [res.subCategoryId]);

                // Reset hình ảnh khi load SKU cũ
                $('#imagePreview').html('<span class="material-symbols-outlined text-slate-200 text-4xl">add_photo_alternate</span>');
                $('#galleryPreview').html('<span class="material-symbols-outlined text-slate-200 text-4xl mt-6">collections</span>');
                $('#imageInput').val('');
                $('#additionalImagesInput').val('');

                showNewProductSection(); // Hiện ra để admin có thể chỉnh sửa nếu cần
            } else {
                icon.html('<span class="material-symbols-outlined text-blue-500">add_circle</span>');
                $('#productInfoAlert').hide();
                resetForm();
                showNewProductSection();
            }
        });
    }

    // Xử lý sự kiện click cho vùng chọn ảnh an toàn hơn, tránh stack overflow
    $(document).on('click', '.trigger-image-input', function (e) {
        if (e.target.id !== 'imageInput') {
            $('#imageInput').click();
        }
    });

    $(document).on('click', '.trigger-gallery-input', function (e) {
        if (e.target.id !== 'additionalImagesInput') {
            $('#additionalImagesInput').click();
        }
    });

    // 2. Xử lý Load Danh mục con
    $('#categorySelect').on('change', function (e, subId) {
        const catId = $(this).val();
        const subSelect = $('#subCategorySelect');

        if (!catId) {
            subSelect.html('<option value="">-- Chọn danh mục con --</option>');
            return;
        }

        $.get('/Admin/Inventory/GetSubCategories', { categoryId: catId }, function (res) {
            let html = '<option value="">-- Chọn danh mục con --</option>';
            res.forEach(item => {
                html += `<option value="${item.id}">${item.name}</option>`;
            });
            subSelect.html(html);

            if (subId) subSelect.val(subId);
        });
    });

    function showNewProductSection() {
        $('.new-product-section').slideDown(400);
    }

    function hideNewProductSection() {
        $('.new-product-section').slideUp(300);
    }

    function resetForm() {
        $('#nameInput, #brandInput, #sellingPriceInput, #descriptionInput, #imageInput, #additionalImagesInput').val('');
        $('#categorySelect').val('');
        $('#subCategorySelect').html('<option value="">-- Chọn danh mục con --</option>');
        $('#imagePreview').html('<span class="material-symbols-outlined text-slate-200 text-4xl">add_photo_alternate</span>');
        $('#galleryPreview').html('<span class="material-symbols-outlined text-slate-200 text-4xl mt-6">collections</span>');
    }

    // Image Preview logic
    $('#imageInput').change(function () {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $('#imagePreview').html(`<img src="${e.target.result}" class="w-full h-full object-cover">`);
            };
            reader.readAsDataURL(file);
        }
    });

    $('#additionalImagesInput').change(function () {
        const files = this.files;
        $('#galleryPreview').html('');
        if (files && files.length > 0) {
            for (let i = 0; i < files.length; i++) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    $('#galleryPreview').append(`<div class="w-16 h-16 rounded-xl overflow-hidden shadow-sm shrink-0"><img src="${e.target.result}" class="w-full h-full object-cover"></div>`);
                };
                reader.readAsDataURL(files[i]);
            }
        } else {
            $('#galleryPreview').html('<span class="material-symbols-outlined text-slate-200 text-4xl mt-6">collections</span>');
        }
    });

    // 3. Xử lý Form Submit (Sử dụng FormData để hỗ trợ Upload File)
    $('#importStockForm').on('submit', function (e) {
        e.preventDefault();
        const btn = $(this).find('button[type="submit"]');
        const originalText = btn.html();

        btn.prop('disabled', true).html('<span class="material-symbols-outlined animate-spin text-sm">refresh</span> Đang xử lý...');

        // Tạo FormData để gửi cả file
        const formData = new FormData(this);

        $.ajax({
            url: '/Admin/Inventory/Import',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                if (res.success) {
                    alert(res.message);
                    window.location.href = '/Admin/Inventory/Index';
                } else {
                    alert(res.message);
                    btn.prop('disabled', false).html(originalText);
                }
            },
            error: function () {
                alert('Có lỗi hệ thống xảy ra');
                btn.prop('disabled', false).html(originalText);
            }
        });
    });
});
