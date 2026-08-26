/**
 * @fileoverview ValidationHelper — Inline form validation for Lumen Admin.
 * Replaces native browser popups with styled inline error messages,
 * fully compatible with Choices.js dropdowns and Tailwind CSS utility classes.
 *
 * @version 2.0.0
 * @author  Lumen Admin
 */

; (function (window, $) {
    'use strict';

    // ─── Constants ────────────────────────────────────────────────────────────

    /** CSS classes applied to an invalid field's visual target. */
    const CSS_INVALID = 'border-red-500 bg-red-50/40 ring-2 ring-red-100';
    /** CSS classes restored when a field becomes valid / is cleared. */
    const CSS_VALID = 'border-slate-200';
    /** CSS classes removed when a field becomes invalid. */
    const CSS_CLEAN = 'border-slate-100 border-slate-200 focus:border-blue-600';
    /** Class used to locate and hide/show error message spans. */
    const CLS_FEEDBACK = 'invalid-feedback';

    /**
     * Input types that should never be validated (they carry no user-facing value).
     * @type {Set<string>}
     */
    const SKIP_TYPES = new Set(['hidden', 'button', 'submit', 'reset', 'file']);

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /**
     * Returns the Choices.js root element wrapping the given <select>,
     * or an empty jQuery set if the field is not managed by Choices.js.
     *
     * Choices.js can render its wrapper either as the next sibling of the
     * original <select> *or* as the direct parent (depending on the version /
     * initialisation order), so we check both locations.
     *
     * @param  {jQuery} $input
     * @return {jQuery} .choices wrapper element, or $()
     */
    function getChoicesWrapper($input) {
        const $next = $input.next('.choices');
        if ($next.length) return $next;

        const $parent = $input.parent('.choices');
        if ($parent.length) return $parent;

        return $();
    }

    /**
     * Builds a fresh error-message <span> ready to be inserted into the DOM.
     * Using `style` for color instead of a Tailwind utility avoids the common
     * pitfall where dynamic class names are purged by the Tailwind JIT compiler
     * or overridden by a higher-specificity rule in the project's stylesheet.
     *
     * @return {jQuery}
     */
    function buildErrorSpan() {
        return $('<span>')
            .addClass(`${CLS_FEEDBACK} text-[10px] font-semibold mt-1.5 block`)
            .css('color', '#e11d48'); // rose-600 — matches LumenAlert palette
    }

    // ─── ValidationHelper ─────────────────────────────────────────────────────

    /**
     * @namespace ValidationHelper
     */
    const ValidationHelper = {

        /** Fallback message when a field provides no custom `data-required-message`. */
        defaultMessage: 'Vui lòng nhập trường này',

        // ── Lifecycle ──────────────────────────────────────────────────────────

        /**
         * Attaches delegated event listeners at the document level.
         * Safe to call multiple times — existing listeners are removed first.
         */
        init() {
            $('form').attr('novalidate', 'novalidate');

            $(document)
                .off('.lumenValidation')
                .on('input.lumenValidation change.lumenValidation',
                    'input, select, textarea',
                    ({ currentTarget }) => ValidationHelper.clearField($(currentTarget))
                )
                .on('click.lumenValidation',
                    'button[type="submit"], input[type="submit"]',
                    ({ currentTarget }) => {
                        if (currentTarget.form) {
                            $(currentTarget.form).attr('novalidate', 'novalidate');
                        }
                    }
                );
        },

        // ── Public API ─────────────────────────────────────────────────────────

        /**
         * Validates all eligible fields within `$form`.
         * Scrolls to and focuses the first invalid field on failure.
         *
         * @param  {jQuery}  $form
         * @return {boolean} `true` when every field passes validation.
         */
        validateForm($form) {
            this.clearErrors($form);

            let isValid = true;

            $form.find('input, select, textarea').each((_, el) => {
                const $input = $(el);
                if (this._shouldSkip($input)) return;

                const error = this._getFieldError($input);
                if (error) {
                    this.showFieldError($input, error);
                    isValid = false;
                }
            });

            if (!isValid) this._focusFirstError($form);

            return isValid;
        },

        /**
         * Renders server-side validation errors returned by the API.
         * Keys may use PascalCase (C# model names) or camelCase; both are resolved.
         *
         * @param {jQuery} $form
         * @param {Object.<string, string|string[]>} errors
         */
        showErrors($form, errors) {
            this.clearErrors($form);
            if (!errors || typeof errors !== 'object') return;

            Object.entries(errors).forEach(([key, raw]) => {
                const message = Array.isArray(raw) ? raw[0] : raw;
                const $input = this._resolveField($form, key);
                if ($input?.length) this.showFieldError($input, message);
            });
        },

        /**
         * Marks a single field as invalid and displays `message` beneath it.
         *
         * @param {jQuery} $input
         * @param {string} [message]
         */
        showFieldError($input, message) {
            if (!$input?.length) return;

            // Highlight the visual element (Choices inner container or the raw input)
            this._getVisualTarget($input)
                .addClass(CSS_INVALID)
                .removeClass(CSS_CLEAN);

            // Reuse an existing span when possible to avoid duplicate nodes
            let $span = this._findErrorSpan($input);
            if (!$span.length) {
                $span = buildErrorSpan();
                this._insertErrorSpan($input, $span);
            }

            $span.text(message || this.defaultMessage).removeClass('hidden');
        },

        /**
         * Removes all validation state (error classes + messages) within `$scope`.
         *
         * @param {jQuery} $scope
         */
        clearErrors($scope) {
            $scope
                .find(`.${CLS_FEEDBACK}`)
                .addClass('hidden')
                .text('');

            $scope
                .find('.border-red-500, .ring-red-100')
                .removeClass(CSS_INVALID)
                .addClass(CSS_VALID);
        },

        /**
         * Clears the validation state for a single field only.
         *
         * @param {jQuery} $input
         */
        clearField($input) {
            this._getVisualTarget($input).removeClass(CSS_INVALID);
            this._findErrorSpan($input).addClass('hidden').text('');
        },

        // ── Private ────────────────────────────────────────────────────────────

        /**
         * Returns a validation error message for the given field, or `null` if valid.
         *
         * @param  {jQuery}      $input
         * @return {string|null}
         */
        _getFieldError($input) {
            const value = this._getValue($input);
            const type = ($input.attr('type') || '').toLowerCase();
            const required = $input.is('[required]') ||
                String($input.data('required')).toLowerCase() === 'true';

            if (required && !value) {
                return $input.data('requiredMessage') || this.defaultMessage;
            }

            // Skip further checks when the field is empty and not required
            if (!value) return null;

            if (type === 'email' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
                return 'Email không đúng định dạng';
            }

            const minLength = parseInt($input.attr('minlength'), 10);
            if (!isNaN(minLength) && value.length < minLength) {
                return `Vui lòng nhập tối thiểu ${minLength} ký tự`;
            }

            const maxLength = parseInt($input.attr('maxlength'), 10);
            if (!isNaN(maxLength) && value.length > maxLength) {
                return `Vui lòng nhập tối đa ${maxLength} ký tự`;
            }

            if (type === 'number' || $input.is('[min], [max]')) {
                const num = Number(value);
                if (Number.isNaN(num)) return 'Giá trị phải là số hợp lệ';

                const min = Number($input.attr('min'));
                if ($input.is('[min]') && num < min) {
                    return `Giá trị phải lớn hơn hoặc bằng ${min}`;
                }

                const max = Number($input.attr('max'));
                if ($input.is('[max]') && num > max) {
                    return `Giá trị phải nhỏ hơn hoặc bằng ${max}`;
                }
            }

            return null;
        },

        /**
         * Returns `true` for fields that must not be validated
         * (disabled, readonly, hidden, or explicitly opted-out via data attribute).
         *
         * @param  {jQuery}  $input
         * @return {boolean}
         */
        _shouldSkip($input) {
            const type = ($input.attr('type') || '').toLowerCase();
            return (
                SKIP_TYPES.has(type) ||
                $input.is(':disabled, [readonly], [data-validate="false"]') ||
                $input.closest('[data-validate="false"]').length > 0
            );
        },

        /**
         * Returns the trimmed string value of a field.
         * For multi-select fields the selected values are joined with a comma.
         *
         * @param  {jQuery} $input
         * @return {string}
         */
        _getValue($input) {
            const val = $input.val();
            return Array.isArray(val)
                ? val.filter(Boolean).join(',')
                : String(val ?? '').trim();
        },

        /**
         * Returns the element that should receive the error highlight classes.
         * For Choices.js-managed selects this is `.choices__inner`;
         * for everything else it is the input itself.
         *
         * @param  {jQuery} $input
         * @return {jQuery}
         */
        _getVisualTarget($input) {
            const $choices = getChoicesWrapper($input);
            return $choices.length ? $choices.find('.choices__inner') : $input;
        },

        /**
         * Locates the existing error-message span associated with `$input`.
         *
         * @param  {jQuery} $input
         * @return {jQuery} The span element, or an empty jQuery set.
         */
        _findErrorSpan($input) {
            const $choices = getChoicesWrapper($input);
            if ($choices.length) return $choices.next(`.${CLS_FEEDBACK}`);
            return $input.nextAll(`.${CLS_FEEDBACK}`).first();
        },

        /**
         * Inserts a new error-message span after the appropriate DOM element.
         *
         * Insertion order of precedence:
         *   1. After the Choices.js wrapper (when present)
         *   2. Appended to the nearest `.field-validation-wrapper` ancestor
         *   3. Directly after the `<input>` / `<select>` / `<textarea>`
         *
         * @param {jQuery} $input
         * @param {jQuery} $span
         */
        _insertErrorSpan($input, $span) {
            const $choices = getChoicesWrapper($input);
            if ($choices.length) {
                $choices.after($span);
                return;
            }

            const $wrapper = $input.closest('.field-validation-wrapper');
            if ($wrapper.length) {
                $wrapper.append($span);
                return;
            }

            $input.after($span);
        },

        /**
         * Resolves a field element from a server-side error key.
         * Tries both PascalCase and camelCase variants as well as common
         * `*Input` id suffixes used in the Lumen admin templates.
         *
         * @param  {jQuery} $form
         * @param  {string} key   — e.g. "Product.Name" or "categoryId"
         * @return {jQuery|null}
         */
        _resolveField($form, key) {
            const base = key.split('.').pop();
            const camel = base.charAt(0).toLowerCase() + base.slice(1);
            const pascal = base.charAt(0).toUpperCase() + base.slice(1);

            const selectors = [
                `[name="${key}"]`, `[id="${key}"]`,
                `[name="${base}"]`, `[id="${base}"]`,
                `[name="${camel}"]`, `[id="${camel}"]`,
                `[name="${pascal}"]`, `[id="${pascal}"]`,
                `[id="${camel}Input"]`,
                `[id="${base.toLowerCase()}Input"]`,
            ].join(', ');

            const $field = $form.find(selectors).first();
            return $field.length ? $field : null;
        },

        /**
         * Focuses the first invalid field in `$form` so the user's attention
         * is drawn to it without requiring a manual scroll.
         *
         * @param {jQuery} $form
         */
        _focusFirstError($form) {
            const $feedback = $form.find(`.${CLS_FEEDBACK}:not(.hidden)`).first();
            if (!$feedback.length) return;

            const $target = $feedback.prevAll('input, select, textarea, .choices').first();
            ($target.length ? $target : $feedback).trigger('focus');
        },
    };

    // ─── Export ───────────────────────────────────────────────────────────────

    const LumenAlert = {
        ensureStyles() {
            if (document.getElementById('lumen-alert-styles')) return;

            const style = document.createElement('style');
            style.id = 'lumen-alert-styles';
            style.textContent = `
                .lumen-alert-popup {
                    width: min(92vw, 480px) !important;
                    border-radius: 28px !important;
                    padding: 28px !important;
                    border: 1px solid rgba(226, 232, 240, 0.9) !important;
                    box-shadow: 0 30px 90px rgba(15, 23, 42, 0.22) !important;
                    font-family: 'Plus Jakarta Sans', Arial, sans-serif !important;
                }
                .lumen-alert-popup.swal2-show {
                    animation: lumenAlertIn 0.28s cubic-bezier(0.16, 1, 0.3, 1) !important;
                }
                .lumen-alert-title {
                    color: #0f172a !important;
                    font-size: 24px !important;
                    font-weight: 900 !important;
                    letter-spacing: 0 !important;
                    padding: 0 !important;
                }
                .lumen-alert-html,
                .lumen-alert-popup .swal2-html-container {
                    color: #475569 !important;
                    font-size: 14px !important;
                    font-weight: 600 !important;
                    line-height: 1.65 !important;
                    margin: 14px 0 0 !important;
                }
                .lumen-alert-confirm,
                .lumen-alert-cancel,
                .lumen-alert-danger {
                    border: 0 !important;
                    border-radius: 16px !important;
                    padding: 12px 22px !important;
                    font-size: 11px !important;
                    font-weight: 900 !important;
                    letter-spacing: 0.08em !important;
                    text-transform: uppercase !important;
                    box-shadow: none !important;
                    outline: none !important;
                }
                .lumen-alert-confirm {
                    background: linear-gradient(135deg, #2563eb, #1d4ed8) !important;
                    color: #fff !important;
                }
                .lumen-alert-danger {
                    background: linear-gradient(135deg, #e11d48, #be123c) !important;
                    color: #fff !important;
                }
                .lumen-alert-cancel {
                    background: #e2e8f0 !important;
                    color: #475569 !important;
                }
                .lumen-alert-popup .swal2-icon {
                    margin-top: 0 !important;
                    border-width: 3px !important;
                }
                html.dark .lumen-alert-popup {
                    background: #0f172a !important;
                    color: #f8fafc !important;
                    border-color: rgba(148, 163, 184, 0.2) !important;
                    box-shadow: 0 30px 90px rgba(0, 0, 0, 0.45) !important;
                }
                html.dark .lumen-alert-title {
                    color: #f8fafc !important;
                }
                html.dark .lumen-alert-html,
                html.dark .lumen-alert-popup .swal2-html-container {
                    color: #cbd5e1 !important;
                }
                html.dark .lumen-alert-cancel {
                    background: #1e293b !important;
                    color: #cbd5e1 !important;
                }
                @keyframes lumenAlertIn {
                    from { opacity: 0; transform: translateY(16px) scale(0.97); }
                    to { opacity: 1; transform: translateY(0) scale(1); }
                }
            `;
            document.head.appendChild(style);
        },

        palette: {
            blue: '#2563eb',
            emerald: '#10b981',
            rose: '#e11d48',
            amber: '#f59e0b',
            slate: '#64748b'
        },

        baseOptions() {
            const isDark = document.documentElement.classList.contains('dark');
            LumenAlert.ensureStyles();
            return {
                background: isDark ? '#0f172a' : '#ffffff',
                color: isDark ? '#f8fafc' : '#0f172a',
                buttonsStyling: false,
                customClass: {
                    popup: 'lumen-alert-popup',
                    title: 'lumen-alert-title',
                    htmlContainer: 'lumen-alert-html',
                    confirmButton: 'lumen-alert-confirm',
                    cancelButton: 'lumen-alert-cancel'
                }
            };
        },

        fire(options) {
            return Swal.fire({
                ...this.baseOptions(),
                ...options,
                customClass: {
                    ...this.baseOptions().customClass,
                    ...(options.customClass || {})
                }
            });
        },

        success(title, text) {
            return LumenAlert.fire({
                icon: 'success',
                title,
                text,
                confirmButtonColor: this.palette.emerald,
                timer: 1600,
                showConfirmButton: false
            });
        },

        error(title, text) {
            return LumenAlert.fire({ icon: 'error', title, text, confirmButtonText: 'Đã hiểu' });
        },

        warning(title, text) {
            return LumenAlert.fire({ icon: 'warning', title, text, confirmButtonText: 'Đã hiểu' });
        },

        confirm(options) {
            return LumenAlert.fire({
                icon: options.icon || 'warning',
                title: options.title || 'Xác nhận?',
                text: options.text,
                html: options.html,
                showCancelButton: true,
                confirmButtonText: options.confirmButtonText || 'Đồng ý',
                cancelButtonText: options.cancelButtonText || 'Hủy',
                customClass: {
                    confirmButton: 'lumen-alert-confirm',
                    cancelButton: 'lumen-alert-cancel'
                }
            });
        },

        danger(options) {
            return LumenAlert.fire({
                icon: 'warning',
                title: options.title || 'Xác nhận?',
                text: options.text,
                showCancelButton: true,
                confirmButtonText: options.confirmButtonText || 'Xóa',
                cancelButtonText: options.cancelButtonText || 'Hủy',
                customClass: {
                    confirmButton: 'lumen-alert-danger',
                    cancelButton: 'lumen-alert-cancel'
                }
            });
        }
    };

    window.ValidationHelper = ValidationHelper;
    window.LumenAlert = LumenAlert;

    $(function () {
        ValidationHelper.init();
    });

}(window, jQuery));
