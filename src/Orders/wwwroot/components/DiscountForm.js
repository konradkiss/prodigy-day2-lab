import { h } from '../dom.js';

/* Applies a discount code to an open order; a new code replaces the one already applied. */
export function DiscountForm({ appliedCode, onApply }) {
  const code = h('input', { type: 'text', placeholder: 'SAVE15', autocomplete: 'off', required: true });
  return h('form', { onSubmit: (e) => { e.preventDefault(); onApply(code.value.trim()); } },
    h('label', {}, 'Discount code', code),
    h('button', { type: 'submit' }, 'Apply'),
    appliedCode && h('span', { class: 'muted' }, `Applied: ${appliedCode}`),
  );
}
