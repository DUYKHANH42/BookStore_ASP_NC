$(document).ready(function () {
    let skuTimer;
    let productList = [];
    let currentProductData = null;

    // Helper: Định dạng tiền tệ
    const formatCurrency = (amount) => {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount).replace('₫', '₫');
    };

    // 1. Xử lý gõ SKU
    $('#skuInput').on('input', function () {
        const sku = $(this).val();
        clearTimeout(skuTimer);

        if (sku.length < 3) {
            $('#productDetailSection').slideUp(300);
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
            $('#productDetailSection').slideDown(400);
            currentProductData = res;

            if (res.exists) {
                icon.html('<span class="material-symbols-outlined text-emerald-500">check_circle</span>');
                $('#productInfoAlert').show().find('.p-name').text(res.name);

                // Điền thông tin cũ và KHÓA field (Read-only)
                setFieldsReadOnly(true);
                $('#nameInput').val(res.name);
                $('#brandInput').val(res.brand);
                $('#sellingPriceInput').val(res.price);
                $('#descriptionInput').val(res.description);
                $('#categorySelect').val(res.categoryId).trigger('change', [res.subCategoryId]);

                // Hiển thị ảnh cũ
                if (res.imageUrl) {
                    const finalPath = res.imageUrl.startsWith('http') ? res.imageUrl : '/uploads/' + res.imageUrl;
                    $('#imagePreview').html(`<img src="${finalPath}" class="w-full h-full object-cover">`);
                } else {
                    $('#imagePreview').html('<span class="material-symbols-outlined text-slate-200">add_photo_alternate</span>');
                }

                if (res.images && res.images.length > 0) {
                    let html = '';
                    res.images.forEach(img => {
                        const path = img.startsWith('http') ? img : '/uploads/' + img;
                        html += `<div class="w-8 h-8 rounded bg-white overflow-hidden shadow-sm"><img src="${path}" class="w-full h-full object-cover"></div>`;
                    });
                    $('#galleryPreview').html(html);
                }
            } else {
                icon.html('<span class="material-symbols-outlined text-blue-500">add_circle</span>');
                $('#productInfoAlert').hide();
                setFieldsReadOnly(false);
                resetProductForm(false); // Không reset SKU
            }
        });
    }

    function setFieldsReadOnly(isReadOnly) {
        const inputs = $('#nameInput, #brandInput, #sellingPriceInput, #categorySelect, #subCategorySelect, #descriptionInput');
        inputs.prop('readonly', isReadOnly).toggleClass('bg-slate-50', isReadOnly).toggleClass('cursor-not-allowed', isReadOnly);
        if (isReadOnly) {
            $('.trigger-image-input, .trigger-gallery-input').addClass('pointer-events-none opacity-50');
            $('#categorySelect, #subCategorySelect').addClass('pointer-events-none');
        } else {
            $('.trigger-image-input, .trigger-gallery-input').removeClass('pointer-events-none opacity-50');
            $('#categorySelect, #subCategorySelect').removeClass('pointer-events-none');
        }
    }
    $('#btnAddToList').on('click', function () {
        const sku = $('#skuInput').val();
        const qty = parseInt($('#qtyInput').val());
        const price = parseFloat($('#priceInput').val());

        if (!sku || sku.length < 3) { alert('Vui lòng nhập SKU hợp lệ'); return; }
        if (!qty || qty <= 0) { alert('Số lượng phải > 0'); return; }
        if (isNaN(price) || price < 0) { alert('Giá nhập không hợp lệ'); return; }

        if (productList.some(p => p.sku === sku)) {
            alert('Sản phẩm này đã có trong danh sách chờ');
            return;
        }

        const product = {
            sku: sku,
            qty: qty,
            importPrice: price,
            name: $('#nameInput').val() || 'Sản phẩm mới',
            exists: currentProductData?.exists || false,
            // Dữ liệu tạo mới nếu có
            brand: $('#brandInput').val(),
            sellingPrice: $('#sellingPriceInput').val(),
            categoryId: $('#categorySelect').val(),
            subCategoryId: $('#subCategorySelect').val(),
            description: $('#descriptionInput').val(),
            // Lưu trữ File object
            mainImage: $('#imageInput')[0].files[0],
            galleryImages: $('#additionalImagesInput')[0].files
        };

        productList.push(product);
        updateTable();
        resetProductForm(true);
        $('#skuInput').val('').focus();
        $('#productDetailSection').slideUp();
        $('.sku-status-icon').html('');
    });

    function updateTable() {
        const tbody = $('#tempProductTable tbody');
        const emptyRow = $('#emptyRow');
        const footer = $('#tableFooter');

        if (productList.length === 0) {
            emptyRow.show();
            footer.addClass('hidden');
            $('#btnSubmitAll').prop('disabled', true).find('.count').text('0');
            return;
        }

        emptyRow.hide();
        footer.removeClass('hidden');
        let html = '';
        let total = 0;

        productList.forEach((p, index) => {
            const subtotal = p.qty * p.importPrice;
            total += subtotal;
            html += `
                <tr class="hover:bg-slate-50 transition-colors">
                    <td class="px-6 py-4">
                        <div class="font-bold text-slate-900">${p.name}</div>
                        <div class="text-[10px] text-slate-400 font-black">${p.sku} ${p.exists ? '<span class="text-emerald-500">[Hệ thống]</span>' : '<span class="text-blue-500">[Mới]</span>'}</div>
                    </td>
                    <td class="px-6 py-4 text-center font-black">${p.qty}</td>
                    <td class="px-6 py-4 text-right font-bold">${formatCurrency(p.importPrice)}</td>
                    <td class="px-6 py-4 text-right font-black text-blue-600">${formatCurrency(subtotal)}</td>
                    <td class="px-6 py-4 text-center">
                        <button type="button" onclick="window.removeProduct(${index})" class="w-8 h-8 flex items-center justify-center rounded-lg text-slate-300 hover:text-red-500 hover:bg-red-50 transition-all">
                            <span class="material-symbols-outlined text-sm">delete</span>
                        </button>
                    </td>
                </tr>
            `;
        });

        tbody.html(html);
        $('#receiptTotalAmount').text(formatCurrency(total));
        $('#totalItemsCount').text(productList.length);
        $('#btnSubmitAll').prop('disabled', false).find('.count').text(productList.length);
    }

    window.removeProduct = function (index) {
        productList.splice(index, 1);
        updateTable();
    };

    // 3. Submit toàn bộ
    $('#importStockForm').on('submit', function (e) {
        e.preventDefault();
        if (productList.length === 0) return;

        const btn = $('#btnSubmitAll');
        const originalText = btn.html();
        btn.prop('disabled', true).html('<span class="material-symbols-outlined animate-spin text-sm">refresh</span> Đang xử lý...');

        const formData = new FormData();
        formData.append('SupplierId', $('#supplierSelect').val());
        formData.append('Notes', $('#receiptNotes').val());

        productList.forEach((p, i) => {
            formData.append(`Items[${i}].SKU`, p.sku);
            formData.append(`Items[${i}].QuantityToImport`, p.qty);
            formData.append(`Items[${i}].ImportPrice`, p.importPrice);

            if (!p.exists) {
                formData.append(`Items[${i}].Name`, p.name);
                formData.append(`Items[${i}].Brand`, p.brand);
                formData.append(`Items[${i}].SellingPrice`, p.sellingPrice);
                formData.append(`Items[${i}].CategoryId`, p.categoryId);
                formData.append(`Items[${i}].SubCategoryId`, p.subCategoryId);
                formData.append(`Items[${i}].Description`, p.description);
                if (p.mainImage) formData.append(`Items[${i}].ImageFile`, p.mainImage);
                if (p.galleryImages) {
                    for (let j = 0; j < p.galleryImages.length; j++) {
                        formData.append(`Items[${i}].AdditionalImageFiles`, p.galleryImages[j]);
                    }
                }
            }
        });

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

    // Các hàm phụ trợ (Preview ảnh, Load Category...)
    $(document).on('click', '.trigger-image-input', function () { $('#imageInput').click(); });
    $(document).on('click', '.trigger-gallery-input', function () { $('#additionalImagesInput').click(); });

    $('#imageInput').change(function () {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = e => $('#imagePreview').html(`<img src="${e.target.result}" class="w-full h-full object-cover">`);
            reader.readAsDataURL(file);
        }
    });

    $('#additionalImagesInput').change(function () {
        const files = this.files;
        $('#galleryPreview').html('');
        for (let i = 0; i < files.length; i++) {
            const reader = new FileReader();
            reader.onload = e => $('#galleryPreview').append(`<div class="w-8 h-8 rounded overflow-hidden shadow-sm"><img src="${e.target.result}" class="w-full h-full object-cover"></div>`);
            reader.readAsDataURL(files[i]);
        }
    });

    $('#categorySelect').on('change', function (e, subId) {
        const catId = $(this).val();
        const subSelect = $('#subCategorySelect');
        if (!catId) { subSelect.html('<option value="">-- Chọn danh mục con --</option>'); return; }

        $.get('/Admin/Inventory/GetSubCategories', { categoryId: catId }, function (res) {
            let html = '<option value="">-- Chọn danh mục con --</option>';
            res.forEach(item => html += `<option value="${item.id}">${item.name}</option>`);
            subSelect.html(html);
            if (subId) subSelect.val(subId);
        });
    });

    function resetProductForm(includeSKU) {
        if (includeSKU) $('#skuInput').val('');
        $('#qtyInput').val(1);
        $('#priceInput').val('');
        $('#nameInput, #brandInput, #sellingPriceInput, #descriptionInput, #imageInput, #additionalImagesInput').val('');
        $('#categorySelect').val('');
        $('#subCategorySelect').html('<option value="">-- Chọn danh mục con --</option>');
        $('#imagePreview').html('<span class="material-symbols-outlined text-slate-200">add_photo_alternate</span>');
        $('#galleryPreview').html('<span class="material-symbols-outlined text-slate-200">collections</span>');
        currentProductData = null;
    }
});
