// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('table.table').forEach((table) => {
        if (table.parentElement?.classList.contains('table-responsive')) {
            return;
        }

        const wrapper = document.createElement('div');
        wrapper.className = 'table-responsive';
        table.parentNode?.insertBefore(wrapper, table);
        wrapper.appendChild(table);
    });
});
