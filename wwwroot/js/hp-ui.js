// HP Auto Detailing - UI Helpers
// Sidebar, Dropdowns, Toasts, Modals, Tabs

(function () {
  'use strict';

  window.HPUI = window.HPUI || {};

  // ========== SIDEBAR ==========
  HPUI.openSidebar = function () {
    var s = document.getElementById('hpSidebar');
    var o = document.getElementById('hpSidebarOverlay');
    if (s) { s.classList.remove('-translate-x-full'); s.classList.add('translate-x-0'); }
    if (o) { o.classList.remove('hidden'); }
  };

  HPUI.closeSidebar = function () {
    var s = document.getElementById('hpSidebar');
    var o = document.getElementById('hpSidebarOverlay');
    if (s) { s.classList.remove('translate-x-0'); s.classList.add('-translate-x-full'); }
    if (o) { o.classList.add('hidden'); }
  };

  // ========== DROPDOWNS ==========
  document.addEventListener('click', function (e) {
    // Toggle dropdown
    var toggle = e.target.closest('[data-hp-dropdown-toggle]');
    if (toggle) {
      var root = toggle.closest('[data-hp-dropdown]');
      if (root) {
        e.preventDefault();
        e.stopPropagation();
        // Close all other dropdowns
        document.querySelectorAll('[data-hp-dropdown].open').forEach(function (dd) {
          if (dd !== root) dd.classList.remove('open');
        });
        root.classList.toggle('open');
        return;
      }
    }
    // Close dropdowns on click outside
    document.querySelectorAll('[data-hp-dropdown].open').forEach(function (dd) {
      if (!dd.contains(e.target)) dd.classList.remove('open');
    });
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
      document.querySelectorAll('[data-hp-dropdown].open').forEach(function (dd) {
        dd.classList.remove('open');
      });
      // Also close any open modals
      document.querySelectorAll('.hp-modal-overlay').forEach(function (m) {
        HPUI.closeModal(m.id);
      });
    }
  });

  // ========== TOASTS ==========
  var _toastId = 0;
  HPUI.toast = function (message, type) {
    type = type || 'info';
    var container = document.getElementById('hpToastContainer');
    if (!container) return;

    _toastId++;
    var id = 'hp-toast-' + _toastId;

    var iconSvg = '';
    switch (type) {
      case 'success':
        iconSvg = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>';
        break;
      case 'error':
        iconSvg = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>';
        break;
      case 'warning':
        iconSvg = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>';
        break;
      default: // info
        iconSvg = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>';
        break;
    }

    var toast = document.createElement('div');
    toast.id = id;
    toast.className = 'hp-toast toast-' + type;
    toast.innerHTML = iconSvg +
      '<span class="flex-1">' + message + '</span>' +
      '<button class="toast-close" onclick="HPUI.dismissToast(\'' + id + '\')">' +
        '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>' +
      '</button>';

    container.appendChild(toast);

    // Auto-dismiss after 4s
    setTimeout(function () { HPUI.dismissToast(id); }, 4000);
  };

  HPUI.dismissToast = function (id) {
    var el = document.getElementById(id);
    if (!el || el.classList.contains('removing')) return;
    el.classList.add('removing');
    setTimeout(function () { if (el.parentNode) el.parentNode.removeChild(el); }, 300);
  };

  // ========== MODALS ==========
  HPUI.openModal = function (modalId) {
    var m = document.getElementById(modalId);
    if (!m) return;
    m.classList.remove('hidden', 'closing');
    m.style.display = '';
    document.body.style.overflow = 'hidden';
  };

  HPUI.closeModal = function (modalId) {
    var m = document.getElementById(modalId);
    if (!m) return;
    m.classList.add('closing');
    setTimeout(function () {
      m.classList.add('hidden');
      m.classList.remove('closing');
      m.style.display = 'none';
      // Restore scroll if no other modals
      if (!document.querySelector('.hp-modal-overlay:not(.hidden)')) {
        document.body.style.overflow = '';
      }
    }, 200);
  };

  // Close modal on backdrop click
  document.addEventListener('click', function (e) {
    if (e.target.classList.contains('hp-modal-overlay')) {
      HPUI.closeModal(e.target.id);
    }
  });

  // ========== TABS ==========
  HPUI.switchTab = function (tabGroupId, tabName) {
    var group = document.querySelector('[data-tab-group="' + tabGroupId + '"]');
    if (!group) return;

    // Update buttons
    group.querySelectorAll('[data-tab-btn]').forEach(function (btn) {
      var isActive = btn.getAttribute('data-tab-btn') === tabName;
      btn.classList.toggle('bg-slate-800', isActive);
      btn.classList.toggle('text-white', isActive);
      btn.classList.toggle('text-slate-400', !isActive);
      btn.classList.toggle('border-blue-500/50', isActive);
      btn.classList.toggle('border-transparent', !isActive);
    });

    // Update panels
    group.querySelectorAll('[data-tab-panel]').forEach(function (panel) {
      var isActive = panel.getAttribute('data-tab-panel') === tabName;
      panel.classList.toggle('hidden', !isActive);
    });
  };
  
  // ========== ACCORDIONS ==========
  document.addEventListener('click', function (e) {
    var header = e.target.closest('.hp-accordion-header');
    if (!header) return;
    
    var accordion = header.closest('.hp-accordion');
    if (!accordion) return;
    
    accordion.classList.toggle('active');
  });

  // ========== GENERIC TOGGLES ==========
  document.addEventListener('click', function (e) {
    var trigger = e.target.closest('[data-hp-toggle]');
    if (!trigger) return;
    
    var targetId = trigger.getAttribute('data-hp-toggle');
    var target = document.getElementById(targetId);
    if (target) {
      var className = trigger.getAttribute('data-hp-toggle-class') || 'hidden';
      target.classList.toggle(className);
    }
  });

  // ========== SEARCH FILTER HELPER ==========
  HPUI.filterTable = function (inputId, tbodyId, statusSelectId) {
    var input = document.getElementById(inputId);
    var tbody = document.getElementById(tbodyId);
    var statusSel = statusSelectId ? document.getElementById(statusSelectId) : null;

    function run() {
      var q = (input ? input.value : '').toLowerCase().trim();
      var st = statusSel ? (statusSel.value || '') : '';

      tbody.querySelectorAll('tr[data-search]').forEach(function (row) {
        var hay = (row.getAttribute('data-search') || '').toLowerCase();
        var rowSt = row.getAttribute('data-status') || '';
        var matchQ = !q || hay.indexOf(q) !== -1;
        var matchSt = !st || rowSt === st;
        row.style.display = (matchQ && matchSt) ? '' : 'none';
      });
    }

    if (input) input.addEventListener('input', run);
    if (statusSel) statusSel.addEventListener('change', run);
  };

  // ========== PAGING RENDER ==========
  HPUI.renderPaging = function (containerId, currentPage, totalPages, onPageChange) {
    var container = document.getElementById(containerId);
    if (!container) return;

    currentPage = Number(currentPage) || 1;
    totalPages = Number(totalPages) || 0;

    if (totalPages <= 1) {
      container.innerHTML = '';
      return;
    }

    var createButton = function (label, page, disabled, active) {
      var btn = document.createElement('button');
      btn.type = 'button';
      btn.textContent = label;
      btn.className = [
        'px-3 py-1.5 text-xs rounded-md border transition-colors',
        active
          ? 'bg-blue-500/20 border-blue-500/50 text-blue-300'
          : 'bg-[#122131] border-slate-700/50 text-slate-300 hover:bg-slate-800/50',
        disabled ? 'opacity-50 cursor-not-allowed hover:bg-[#122131]' : ''
      ].join(' ');

      if (!disabled) {
        btn.addEventListener('click', function () {
          if (typeof onPageChange === 'function') {
            onPageChange(page);
          }
        });
      } else {
        btn.disabled = true;
      }

      return btn;
    };

    var wrapper = document.createElement('div');
    wrapper.className = 'flex items-center justify-between gap-3 flex-wrap';

    var info = document.createElement('div');
    info.className = 'text-xs text-slate-400';
    info.textContent = 'Trang ' + currentPage + ' / ' + totalPages;

    var controls = document.createElement('div');
    controls.className = 'flex items-center gap-1.5';
    controls.appendChild(createButton('Truoc', currentPage - 1, currentPage <= 1, false));

    var maxVisible = 5;
    var start = Math.max(1, currentPage - Math.floor(maxVisible / 2));
    var end = Math.min(totalPages, start + maxVisible - 1);
    start = Math.max(1, end - maxVisible + 1);

    for (var page = start; page <= end; page++) {
      controls.appendChild(createButton(String(page), page, false, page === currentPage));
    }

    controls.appendChild(createButton('Sau', currentPage + 1, currentPage >= totalPages, false));

    wrapper.appendChild(info);
    wrapper.appendChild(controls);

    container.innerHTML = '';
    container.appendChild(wrapper);
  };

  // ========== THEME MANAGEMENT ==========
  HPUI.setThemeColor = function (colorName) {
    const colors = {
      'Blue': { main: '#3b82f6', dark: '#2563eb', rgb: '59, 130, 246' },
      'Emerald': { main: '#10b981', dark: '#059669', rgb: '16, 185, 129' },
      'Amber': { main: '#f59e0b', dark: '#d97706', rgb: '245, 158, 11' },
      'Rose': { main: '#f43f5e', dark: '#e11d48', rgb: '244, 63, 94' },
      'Purple': { main: '#a855f7', dark: '#9333ea', rgb: '168, 85, 247' }
    };

    const theme = colors[colorName];
    if (!theme) return;

    document.documentElement.style.setProperty('--primary-color', theme.main);
    document.documentElement.style.setProperty('--primary-color-dark', theme.dark);
    document.documentElement.style.setProperty('--primary-rgb', theme.rgb);
    document.documentElement.style.setProperty('--primary-color-light', `rgba(${theme.rgb}, 0.1)`);
    
    localStorage.setItem('hp-primary-color', colorName);
    HPUI.toast('Đã cập nhật màu giao diện: ' + colorName, 'success');
  };

  HPUI.setThemeMode = function (mode) {
    if (mode === 'light') {
      document.documentElement.classList.add('light-mode');
    } else {
      document.documentElement.classList.remove('light-mode');
    }
    localStorage.setItem('hp-theme-mode', mode);
    HPUI.toast('Đã chuyển sang chế độ ' + (mode === 'light' ? 'Sáng' : 'Tối'), 'info');
    
    // Update active UI state if on settings page
    if (window.updateSettingsUI) window.updateSettingsUI();
  };

  HPUI.setLanguage = function (lang) {
    localStorage.setItem('hp-language', lang);
    HPUI.toast('Đã đổi ngôn ngữ sang: ' + (lang === 'vi' ? 'Tiếng Việt' : 'English'), 'success');
    
    // Update active UI state if on settings page
    if (window.updateSettingsUI) window.updateSettingsUI();
  };

  // ========== RE-INITIALIZATION ==========
  HPUI.reinit = function (container) {
    var scope = container || document;
    
    // 1. Lucide Icons
    if (window.lucide) {
      lucide.createIcons({
        attrs: { class: 'lucide-icon' },
        nameAttr: 'data-lucide'
      });
    }

    // 2. Custom Initialization (can be extended)
    // Example: Tooltips, Popovers, etc.
  };

  // ========== VIETQR GENERATOR ==========
  HPUI.generateQR = function (amount, description, bankId, accountNo) {
    bankId = bankId || 'ICB'; // Default VietinBank
    accountNo = accountNo || '123456789';
    var template = 'compact2'; // or 'qr_only', 'compact'
    
    var url = 'https://img.vietqr.io/image/' + bankId + '-' + accountNo + '-' + template + '.png' +
              '?amount=' + amount +
              '&addInfo=' + encodeURIComponent(description) +
              '&accountName=HP%20AUTO%20DETAILING';
              
    return url;
  };

  // ========== AJAX NAVIGATION ==========
  HPUI.loadPage = function (url, pushToHistory) {
    if (pushToHistory === undefined) pushToHistory = true;
    
    // Show loading bar
    var progress = document.getElementById('hpNavProgress');
    if (progress) {
      progress.style.width = '30%';
      progress.classList.remove('hidden', 'opacity-0');
    }

    fetch(url)
      .then(response => {
        if (!response.ok) throw new Error('Network response was not ok');
        return response.text();
      })
      .then(html => {
        if (progress) progress.style.width = '70%';
        
        var parser = new DOMParser();
        var doc = parser.parseFromString(html, 'text/html');
        
        // 1. Update Title
        document.title = doc.title;
        
        // 2. Replace Main Content
        var newMain = doc.querySelector('main');
        var currentMain = document.querySelector('main');
        if (newMain && currentMain) {
          // Add fade-out effect
          currentMain.style.opacity = '0';
          currentMain.style.transform = 'translateY(5px)';
          currentMain.style.transition = 'all 0.2s ease-out';
          
          setTimeout(() => {
            currentMain.innerHTML = newMain.innerHTML;
            currentMain.className = newMain.className;
            
            // Re-run scripts in main
            currentMain.querySelectorAll('script').forEach(oldScript => {
              const newScript = document.createElement('script');
              Array.from(oldScript.attributes).forEach(attr => newScript.setAttribute(attr.name, attr.value));
              newScript.appendChild(document.createTextNode(oldScript.innerHTML));
              oldScript.parentNode.replaceChild(newScript, oldScript);
            });

            // Fade back in
            currentMain.style.opacity = '1';
            currentMain.style.transform = 'translateY(0)';
            
            // 3. Re-init UI (Centralized)
            HPUI.reinit(currentMain);
            
            HPUI.updateSidebarActiveState(url);
            
            if (progress) {
              progress.style.width = '100%';
              setTimeout(() => {
                progress.classList.add('opacity-0');
                setTimeout(() => { progress.classList.add('hidden'); progress.style.width = '0'; }, 300);
              }, 200);
            }
          }, 200);
        }

        // 4. Update URL
        if (pushToHistory) {
          history.pushState({ url: url }, doc.title, url);
        }
      })
      .catch(error => {
        console.error('AJAX Load Error:', error);
        window.location.href = url; // Fallback to normal load
      });
  };

  HPUI.updateSidebarActiveState = function (url) {
    var path = new URL(url, window.location.origin).pathname;
    document.querySelectorAll('#hpSidebar nav a').forEach(a => {
      var href = a.getAttribute('href');
      var active = href === '/' ? (path === '/' || path === '') : path.startsWith(href);
      
      // Update classes
      if (active) {
        a.classList.add('bg-blue-500/10', 'text-white');
        a.classList.remove('text-slate-400');
        if (!a.querySelector('.hp-active-indicator')) {
          var indicator = document.createElement('span');
          indicator.className = 'hp-active-indicator absolute left-0 top-1/2 -translate-y-1/2 w-1 h-6 bg-blue-500 rounded-r-md shadow-[0_0_8px_rgba(59,130,246,0.5)]';
          a.appendChild(indicator);
        }
      } else {
        a.classList.remove('bg-blue-500/10', 'text-white');
        a.classList.add('text-slate-400');
        var indicator = a.querySelector('.hp-active-indicator');
        if (indicator) indicator.remove();
      }
      
      // Update icons
      var icon = a.querySelector('i');
      if (icon) {
        if (active) {
          icon.classList.add('text-blue-400');
          icon.classList.remove('text-slate-500');
        } else {
          icon.classList.remove('text-blue-400');
          icon.classList.add('text-slate-500');
        }
      }
    });
  };

  // Intercept link clicks
  document.addEventListener('click', function (e) {
    var a = e.target.closest('a');
    if (!a) return;
    
    var href = a.getAttribute('href');
    if (!href || href.startsWith('#') || href.startsWith('javascript:') || a.getAttribute('target') === '_blank') return;
    
    // Internal link check
    if (a.hostname === window.location.hostname) {
      e.preventDefault();
      HPUI.loadPage(href);
      HPUI.closeSidebar(); // Close on mobile if open
    }
  });

  window.addEventListener('popstate', function (e) {
    HPUI.loadPage(window.location.href, false);
  });

  // ========== INIT UI ON START ==========
  document.addEventListener('DOMContentLoaded', function () {
    HPUI.reinit();
  });

})();
