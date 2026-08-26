$(document).ready(function () {
    let skuTimer;
    let productList = [];
    let currentProductData = null;

    // Helper: Định dạng tiền tệ
    const formatCurrency = (amount) => {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount).replace('₫', '₫');
    };

    const resolveImageUrl = (url) => {
        if (!url) return '';
        if (url.startsWith('http')) return url;
        if (url.startsWith('/uploads/')) return url;
        const clean = url.replace(/^\/+/, '').replace(/^uploads\//i, '');
        return '/uploads/' + clean;
    };

    // 1. Xử lý gõ SKU
    $('#skuInput').on('input', function () {
        const sku = $(this).val();
        clearTimeout(skuTimer);

        if (sku.length < 3) {
            $('#productDetailSection').slideUp(300, function () { $(this).addClass('hidden'); });
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
            $('#productDetailSection').removeClass('hidden').hide().slideDown(400);
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
                $('#categorySelect').val(res.categoryId);
                loadSubCategories(res.categoryId, res.subCategoryId);

                // Hiển thị ảnh cũ
                if (res.imageUrl) {
                    $('#imagePreview').html(`<img src="${resolveImageUrl(res.imageUrl)}" class="w-full h-full object-cover">`);
                } else {
                    $('#imagePreview').html('<span class="material-symbols-outlined text-slate-200">add_photo_alternate</span>');
                }

                if (res.images && res.images.length > 0) {
                    let html = '';
                    res.images.forEach(img => {
                        html += `<div class="w-8 h-8 rounded bg-white overflow-hidden shadow-sm"><img src="${resolveImageUrl(img)}" class="w-full h-full object-cover"></div>`;
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
        ValidationHelper.clearErrors($('#addProductSection'));

        const sku = $('#skuInput').val();
        const qty = parseInt($('#qtyInput').val());
        const price = parseFloat($('#priceInput').val());

        if (!sku || sku.length < 3) { 
            ValidationHelper.showFieldError($('#skuInput'), 'Vui lòng nhập SKU hợp lệ (ít nhất 3 ký tự)'); 
            return; 
        }
        if (!qty || qty <= 0) { 
            ValidationHelper.showFieldError($('#qtyInput'), 'Số lượng phải lớn hơn 0'); 
            return; 
        }
        if (isNaN(price) || price < 0) { 
            ValidationHelper.showFieldError($('#priceInput'), 'Giá nhập không hợp lệ'); 
            return; 
        }

        if (productList.some(p => p.sku === sku)) {
            ValidationHelper.showFieldError($('#skuInput'), 'Sản phẩm này đã có trong danh sách chờ');
            return;
        }

        if (!currentProductData?.exists) {
            let isNewProductValid = true;
            if (!$('#nameInput').val().trim()) {
                ValidationHelper.showFieldError($('#nameInput'), 'Vui lòng nhập tên sản phẩm mới');
                isNewProductValid = false;
            }
            if (!$('#brandInput').val().trim()) {
                ValidationHelper.showFieldError($('#brandInput'), 'Vui lòng nhập thương hiệu');
                isNewProductValid = false;
            }
            if (!$('#sellingPriceInput').val() || parseFloat($('#sellingPriceInput').val()) < 1000) {
                ValidationHelper.showFieldError($('#sellingPriceInput'), 'Giá bán phải lớn hơn hoặc bằng 1000');
                isNewProductValid = false;
            }
            if (!$('#categorySelect').val()) {
                ValidationHelper.showFieldError($('#categorySelect'), 'Vui lòng chọn danh mục');
                isNewProductValid = false;
            }
            if (!isNewProductValid) return;
        }

        const product = {
            sku: sku,
            qty: qty,
            importPrice: price,
            name: $('#nameInput').val(),
            exists: currentProductData?.exists || false,
            // Dữ liệu tạo mới nếu có
            brand: $('#brandInput').val(),
            sellingPrice: $('#sellingPriceInput').val(),
            categoryId: $('#categorySelect').val(),
            subCategoryId: $('#subCategorySelect').val(),
            description: $('#descriptionInput').val(),
            // Lưu trữ File object
            mainImage: $('#inventoryMainImage')[0].files[0],
            galleryImages: $('#inventoryGalleryImages')[0].files
        };

        productList.push(product);
        updateTable();
        resetProductForm(true);
        $('#skuInput').val('').focus();
        $('#productDetailSection').slideUp(300, function () { $(this).addClass('hidden'); });
        $('.sku-status-icon').html('');
    });

    function filterSubCategoriesLocal(catId, selectedSubId) {
        const subSelect = $('#subCategorySelect');
        const cat = String(catId || '');

        subSelect.find('option').each(function () {
            const $opt = $(this);
            if (!$opt.val()) {
                $opt.show();
                return;
            }
            $opt.toggle(String($opt.data('category')) === cat);
        });

        if (selectedSubId && subSelect.find(`option[value="${selectedSubId}"]:visible`).length) {
            subSelect.val(String(selectedSubId));
        } else {
            subSelect.val('');
        }
    }

    function loadSubCategories(catId, selectedSubId) {
        const subSelect = $('#subCategorySelect');
        if (!catId) {
            subSelect.val('');
            subSelect.find('option').show();
            return;
        }

        if (subSelect.find('option[data-category]').length > 0) {
            filterSubCategoriesLocal(catId, selectedSubId);
            return;
        }

        $.get('/Admin/Inventory/GetSubCategories', { categoryId: catId, parentId: catId })
            .done(function (res) {
                const items = Array.isArray(res) ? res : [];
                let html = '<option value="">-- Chọn danh mục con --</option>';
                items.forEach(item => {
                    const id = item.id ?? item.Id;
                    const name = item.name ?? item.Name;
                    const parent = item.categoryId ?? item.CategoryId ?? catId;
                    html += `<option value="${id}" data-category="${parent}">${name}</option>`;
                });
                subSelect.html(html);
                if (selectedSubId) subSelect.val(String(selectedSubId));
            })
            .fail(function () {
                console.error('GetSubCategories failed for categoryId:', catId);
            });
    }
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
        ValidationHelper.clearErrors($('#importStockForm'));

        if (!$('#supplierSelect').val()) {
            ValidationHelper.showFieldError($('#supplierSelect'), 'Vui lòng chọn nhà cung cấp');
            return;
        }

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
                    LumenAlert.success('Thành công', res.message).then(() => {
                        window.location.href = '/Admin/Inventory/Index';
                    });
                } else {
                    if (res.errors) {
                        ValidationHelper.showErrors($('#importStockForm'), res.errors);
                        toastr.error('Vui lòng kiểm tra lại thông tin các trường bị đỏ');
                    } else {
                        toastr.error(res.message || 'Không thể tạo phiếu nhập');
                    }
                    btn.prop('disabled', false).html(originalText);
                }
            },
            error: function () {
                toastr.error('Có lỗi hệ thống xảy ra');
                btn.prop('disabled', false).html(originalText);
            }
        });
    });

    // Các hàm phụ trợ (Preview ảnh, Load Category...)


    $('#inventoryMainImage').on('change', function (e) {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = e => {
                $('#imagePreview').empty().append(`<img src="${e.target.result}" class="w-full h-full object-cover">`);
            };
            reader.readAsDataURL(file);
        }
    });

    $('#inventoryGalleryImages').on('change', function (e) {
        const files = this.files;
        if (files && files.length > 0) {
            $('#galleryPreview').empty();
            Array.from(files).forEach(file => {
                const reader = new FileReader();
                reader.onload = e => {
                    $('#galleryPreview').append(`<div class="w-8 h-8 rounded bg-white overflow-hidden shadow-sm border border-slate-100"><img src="${e.target.result}" class="w-full h-full object-cover"></div>`);
                };
                reader.readAsDataURL(file);
            });
        }
    });
    $('#categorySelect').on('change', function () {
        loadSubCategories($(this).val(), null);
    });

    function resetProductForm(includeSKU) {
        if (includeSKU) $('#skuInput').val('');
        $('#qtyInput').val(1);
        $('#priceInput').val('');
        $('#nameInput, #brandInput, #sellingPriceInput, #descriptionInput, #inventoryMainImage, #inventoryGalleryImages').val('');
        $('#categorySelect').val('');
        $('#subCategorySelect').val('');
        $('#subCategorySelect option').show();
        $('#imagePreview').html('<span class="material-symbols-outlined text-slate-200">add_photo_alternate</span>');
        $('#galleryPreview').html('<span class="material-symbols-outlined text-slate-200">collections</span>');
        currentProductData = null;
    }
});
