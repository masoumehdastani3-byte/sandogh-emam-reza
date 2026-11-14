window.GoodLoan = (function () {

  const api = '';



  function $(id) {

    return document.getElementById(id);

  }



  function ready(fn) {

    if (document.readyState === 'loading') {

      document.addEventListener('DOMContentLoaded', fn);

    } else {

      fn();

    }

  }



  function pick(obj, key) {

    if (!obj) return undefined;

    if (obj[key] !== undefined) return obj[key];

    const pascal = key.charAt(0).toUpperCase() + key.slice(1);

    return obj[pascal];

  }



  function hasApiEnvelope(obj) {

    return obj && (Object.prototype.hasOwnProperty.call(obj, 'success') ||

      Object.prototype.hasOwnProperty.call(obj, 'Success'));

  }



  function normalizeApi(raw, resOk) {

    if (raw == null) {

      return { success: !!resOk, message: resOk ? '' : 'پاسخ سرور نامعتبر است', data: null };

    }

    if (Array.isArray(raw)) {

      return { success: !!resOk, message: '', data: raw };

    }

    if (typeof raw !== 'object') {

      return { success: !!resOk, message: String(raw), data: raw };

    }

    if (hasApiEnvelope(raw)) {

      return {

        success: !!pick(raw, 'success'),

        message: pick(raw, 'message') || '',

        data: pick(raw, 'data') ?? pick(raw, 'Data') ?? null

      };

    }

    return { success: resOk !== false, message: '', data: raw };

  }



  function payload(result) {

    if (!result) return null;

    return result.data ? result.data.data : null;

  }



  function showMsg(el, text, ok) {

    if (!el) return;

    el.textContent = text || '';

    el.className = 'msg show ' + (ok ? 'ok' : 'err');

  }

  function escapeDialogText(text) {
    return String(text == null ? '' : text)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function confirmDialog(options) {
    options = options || {};
    return new Promise(function (resolve) {
      const hasInput = !!options.input;
      const rows = options.rows || [];
      const rowHtml = rows.map(function (row) {
        const valueClass = row.strong ? ' class="is-strong"' : '';
        return '<div class="gl-dialog-row"><span>' + escapeDialogText(row.label) + '</span><b' + valueClass + '>' + escapeDialogText(row.value) + '</b></div>';
      }).join('');
      const input = options.input || {};
      const inputHtml = hasInput
        ? '<label class="gl-dialog-field">' + escapeDialogText(input.label || '') +
          '<textarea data-act="input" rows="3" placeholder="' + escapeDialogText(input.placeholder || '') + '">' + escapeDialogText(input.value || '') + '</textarea>' +
          '<span class="gl-dialog-error" data-act="error"></span></label>'
        : '';

      const overlay = document.createElement('div');
      overlay.className = 'gl-dialog-backdrop';
      overlay.innerHTML =
        '<div class="gl-dialog" role="dialog" aria-modal="true">' +
          '<h3>' + escapeDialogText(options.title || 'تأیید') + '</h3>' +
          (options.text ? '<p>' + escapeDialogText(options.text) + '</p>' : '') +
          (rowHtml ? '<div class="gl-dialog-rows">' + rowHtml + '</div>' : '') +
          inputHtml +
          '<div class="gl-dialog-actions">' +
            '<button type="button" class="btn primary" data-act="ok">' + escapeDialogText(options.confirmText || 'تأیید') + '</button>' +
            '<button type="button" class="btn quiet" data-act="cancel">' + escapeDialogText(options.cancelText || 'انصراف') + '</button>' +
          '</div>' +
        '</div>';

      function finish(value) {
        document.removeEventListener('keydown', onKey);
        overlay.remove();
        resolve(value);
      }

      function accept() {
        if (!hasInput) {
          finish(true);
          return;
        }
        const field = overlay.querySelector('[data-act="input"]');
        const value = (field.value || '').trim();
        if (input.required && !value) {
          overlay.querySelector('[data-act="error"]').textContent = 'این قسمت را بنویسید.';
          field.focus();
          return;
        }
        finish(value);
      }

      function onKey(e) {
        if (e.key === 'Escape') finish(hasInput ? null : false);
        if (e.key === 'Enter' && !hasInput) accept();
      }

      overlay.addEventListener('click', function (e) {
        if (e.target === overlay) finish(hasInput ? null : false);
        if (e.target.closest('[data-act="ok"]')) accept();
        if (e.target.closest('[data-act="cancel"]')) finish(hasInput ? null : false);
      });
      document.addEventListener('keydown', onKey);
      document.body.appendChild(overlay);
      const focusTarget = overlay.querySelector('[data-act="input"]') || overlay.querySelector('[data-act="ok"]');
      if (focusTarget) focusTarget.focus();
    });
  }



  function toEnglishDigits(str) {
    return String(str || '')
      .replace(/[۰-۹]/g, function (d) { return '۰۱۲۳۴۵۶۷۸۹'.indexOf(d); })
      .replace(/[٠-٩]/g, function (d) { return '٠١٢٣٤٥٦٧٨٩'.indexOf(d); });
  }

  function parseAmount(value) {
    if (value == null || value === '') return 0;
    const cleaned = toEnglishDigits(value).replace(/[^\d]/g, '');
    return cleaned ? Number(cleaned) : 0;
  }

  function formatNumber(n) {
    const num = parseAmount(n);
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  }

  function money(n) {
    return formatNumber(n) + ' ریال';
  }

  function shamsiDate(item, shamsiKey, dateKey) {
    const shamsi = pick(item, shamsiKey);
    if (shamsi) return shamsi;
    const raw = pick(item, dateKey);
    if (!raw) return '-';
    const dt = new Date(raw);
    if (isNaN(dt.getTime())) return '-';
    return dt.toLocaleDateString('fa-IR');
  }

  function loanStatusText(loan) {
    return pick(loan, 'loanRequestStatusName') ||
      pick(loan, 'loanStatusName') ||
      pick(loan, 'loanRequestStatus') ||
      '-';
  }

  function renderLoanTableRow(loan, options) {
    options = options || {};
    const loanId = pick(loan, 'loanId');
    const user = pick(loan, 'user');
    let html = '<tr>';
    if (options.showQueue) html += '<td>' + (pick(loan, 'queuePosition') || '-') + '</td>';
    html += '<td>' + loanId + '</td>';
    if (options.showMember) html += '<td>' + (pick(user, 'fullName') || pick(loan, 'userId') || '') + '</td>';
    html += '<td class="amount">' + money(pick(loan, 'loanAmount')) + '</td>';
    html += '<td>' + shamsiDate(loan, 'requestDate_Shamsi', 'requestDate') + '</td>';
    if (options.showPaymentDate) html += '<td>' + shamsiDate(loan, 'paymentDate_Shamsi', 'paymentDate') + '</td>';
    if (options.showStatus) html += '<td>' + loanStatusText(loan) + '</td>';
    if (options.showActions || options.showMove) {
      html += '<td class="table-actions queue-actions">';
      if (options.showMove) {
        html += '<button type="button" class="btn dark btn-queue-move" data-id="' + loanId + '" data-dir="-1">جلوتر</button>';
        html += '<button type="button" class="btn dark btn-queue-move" data-id="' + loanId + '" data-dir="1">عقب‌تر</button>';
      }
      if (options.showActions) {
        html += '<button type="button" class="btn primary btn-disburse" data-id="' + loanId + '">وام را بده</button>';
        html += '<button type="button" class="btn dark btn-reject" data-id="' + loanId + '">رد کن</button>';
      }
      html += '</td>';
    }
    html += '</tr>';
    return html;
  }

  function setAmountValue(id, n) {
    const el = $(id);
    if (!el) return;
    el.value = n ? formatNumber(n) : '';
  }

  function getAmountValue(id) {
    const el = $(id);
    return el ? parseAmount(el.value) : 0;
  }

  function bindAmountInputs(selector) {
    document.querySelectorAll(selector || '.amount-input').forEach(function (el) {
      if (el.dataset.amountBound) return;
      el.dataset.amountBound = '1';
      el.setAttribute('inputmode', 'numeric');
      el.setAttribute('autocomplete', 'off');

      el.addEventListener('input', function () {
        const raw = parseAmount(el.value);
        const end = el.value.length === el.selectionEnd;
        el.value = raw ? formatNumber(raw) : '';
        if (end) {
          el.selectionStart = el.selectionEnd = el.value.length;
        }
      });

      el.addEventListener('blur', function () {
        if (el.value) el.value = formatNumber(parseAmount(el.value));
      });
    });
  }



  function getUserId() {

    return Number(localStorage.getItem('gl_userId') || new URLSearchParams(location.search).get('userId') || 0);

  }



  function setSession(user) {

    const id = pick(user, 'userId');

    if (!id) return;

    localStorage.setItem('gl_userId', id);

    localStorage.setItem('gl_fullName', pick(user, 'fullName') || '');

    localStorage.setItem('gl_roleId', pick(user, 'roleId') || '');
    localStorage.setItem('gl_roleName', pick(user, 'roleName') || pick(user.userRole, 'roleName') || '');
    localStorage.setItem('gl_userStatus', pick(user, 'userStatus') || '');
    localStorage.setItem('gl_nationalCode', normalizeNationalCode(pick(user, 'nationalCode')));

  }



  function requireLogin(redirectTo) {

    const userId = getUserId();

    if (userId) return userId;

    location.href = redirectTo || 'login.html?return=' + encodeURIComponent(location.pathname.split('/').pop() + location.search);

    return 0;

  }

  function isManagerRole(roleId, roleName, userStatus) {
    if (Number(userStatus) === 5) return true;
    if (roleName === 'مدیر صندوق' || String(roleName).toLowerCase() === 'admin') return true;
    return Number(roleId) === 1;
  }

  var MULTI_ROLE_NATIONAL_CODE = '0058715940';

  function normalizeNationalCode(value) {
    var digits = toEnglishDigits(String(value || '')).replace(/\D/g, '');
    if (digits.length > 0 && digits.length < 10) digits = ('0000000000' + digits).slice(-10);
    return digits;
  }

  function isMultiRoleAccount() {
    return normalizeNationalCode(localStorage.getItem('gl_nationalCode')) === MULTI_ROLE_NATIONAL_CODE;
  }

  function isDonorRole(roleId, roleName, userStatus) {
    if (isManagerRole(roleId, roleName, userStatus)) return false;
    var name = String(roleName || '').trim();
    if (name === 'خیر' || name.toLowerCase() === 'donator') return true;
    return Number(userStatus) === 1;
  }

  function panelIsDonor(user) {
    var entry = localStorage.getItem('gl_entry');
    if (entry === 'donor') return true;
    if (entry === 'member' || entry === 'admin') return false;
    return isDonorRole(pick(user, 'roleId'), pick(user, 'roleName') || pick(user.userRole, 'roleName'), pick(user, 'userStatus'));
  }

  function entryLabel(mode) {
    if (mode === 'admin') return 'مدیر صندوق';
    if (mode === 'donor') return 'خیر';
    return 'وام‌گیرنده';
  }

  function applyEntry(mode) {
    localStorage.setItem('gl_entry', mode);
    var target = mode === 'admin' ? 'admin.html' : 'panel.html';
    if (location.pathname.toLowerCase().endsWith(target)) location.reload();
    else location.href = target;
  }

  function mountEntrySwitcher(host) {
    if (!host || !isMultiRoleAccount()) return;
    var current = localStorage.getItem('gl_entry') || 'member';
    host.style.display = 'block';
    host.innerHTML =
      '<p class="entry-now">الان به‌عنوان <b>' + entryLabel(current) + '</b> هستید. نقش را عوض کنید:</p>' +
      '<div class="entry-actions">' +
      '<button type="button" data-entry="admin">مدیر صندوق</button>' +
      '<button type="button" data-entry="member">وام‌گیرنده</button>' +
      '<button type="button" data-entry="donor">خیر</button>' +
      '</div>';
    Array.prototype.forEach.call(host.querySelectorAll('[data-entry]'), function (btn) {
      if (btn.getAttribute('data-entry') === current) btn.classList.add('active');
      btn.addEventListener('click', function () {
        applyEntry(btn.getAttribute('data-entry'));
      });
    });
  }

  async function requireManager() {
    const userId = getUserId();
    if (!userId) {
      location.href = 'login.html?return=admin.html';
      return 0;
    }

    const cachedRoleId = localStorage.getItem('gl_roleId');
    const cachedRoleName = localStorage.getItem('gl_roleName') || '';
    const cachedUserStatus = localStorage.getItem('gl_userStatus');
    if (isManagerRole(cachedRoleId, cachedRoleName, cachedUserStatus)) return userId;

    const result = await get('/api/user/' + userId);
    if (!result.res.ok) {
      location.href = 'login.html?return=admin.html';
      return 0;
    }

    const user = payload(result);
    const roleId = pick(user, 'roleId');
    const roleName = pick(user, 'roleName') || pick(user.userRole, 'roleName');
    const userStatus = pick(user, 'userStatus');
    localStorage.setItem('gl_roleId', roleId || '');
    localStorage.setItem('gl_roleName', roleName || '');
    localStorage.setItem('gl_userStatus', userStatus || '');

    if (isManagerRole(roleId, roleName, userStatus)) return userId;
    return 0;
  }



  async function request(method, url, body) {

    try {

      const options = {

        method: method,

        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' }

      };

      if (body !== undefined) options.body = JSON.stringify(body);



      const res = await fetch(api + url, options);

      let raw = null;

      const text = await res.text();

      if (text) {

        try {

          raw = JSON.parse(text);

        } catch (e) {

          raw = { message: text };

        }

      }



      const data = normalizeApi(raw, res.ok);

      if (!data.message && !res.ok) {

        data.success = false;

        data.message = res.status === 401 ? 'ورود نامعتبر است' : ('خطای سرور: ' + res.status);

      }

      return { res: res, data: data, raw: raw };

    } catch (err) {

      return {

        res: { ok: false, status: 0 },

        data: {

          success: false,

          message: 'اتصال به سرور برقرار نشد. برنامه را با dotnet run اجرا کنید و از آدرس http://localhost:5000 باز کنید (نه با دوبارکلیک روی فایل HTML).',

          data: null

        },

        raw: null

      };

    }

  }



  async function post(url, body) { return request('POST', url, body); }
  async function put(url, body) { return request('PUT', url, body); }
  async function get(url) { return request('GET', url); }
  async function del(url) { return request('DELETE', url); }



  function openTab(tabId) {

    if (!tabId) return;

    document.querySelectorAll('.tab-panel').forEach(function (p) { p.classList.remove('active'); });

    document.querySelectorAll('.tab-menu .tab-btn').forEach(function (b) { b.classList.remove('active'); });

    const panel = document.getElementById(tabId);

    const btn = document.querySelector('.tab-menu .tab-btn[data-tab="' + tabId + '"]');

    if (panel) panel.classList.add('active');

    if (btn) btn.classList.add('active');

    scrollActiveTabIntoView();

  }



  function initTabs(defaultTabId) {

    const menu = document.querySelector('.tab-menu');

    if (menu && !menu.dataset.bound) {

      menu.addEventListener('click', function (e) {

        const btn = e.target.closest('.tab-btn');

        if (!btn || !menu.contains(btn)) return;

        e.preventDefault();

        openTab(btn.getAttribute('data-tab'));

      });

      menu.dataset.bound = '1';

    }

    const fromQuery = new URLSearchParams(location.search).get('tab');

    const first = menu && menu.querySelector('.tab-btn');

    const target = fromQuery || defaultTabId || (first && first.getAttribute('data-tab'));

    if (target) openTab(target);

  }



  function bindClick(id, handler) {

    const el = $(id);

    if (!el) return;

    el.addEventListener('click', function (e) {

      e.preventDefault();

      handler(e);

    });

  }



  function bindFormEnter(formIds, buttonId) {

    formIds.forEach(function (id) {

      const el = $(id);

      if (!el) return;

      el.addEventListener('keydown', function (e) {

        if (e.key === 'Enter') {

          e.preventDefault();

          const btn = $(buttonId);

          if (btn) btn.click();

        }

      });

    });

  }

  var ADMIN_NAV_ITEMS = [
    { heading: 'کار روزانه' },
    { href: 'admin.html?tab=tab-queue', icon: 'fa-stream', label: 'صف وام', match: function (p, t) { return p === 'admin' && (!t || t === 'tab-queue'); } },
    { href: 'admin.html?tab=tab-manage', icon: 'fa-cogs', label: 'کارهای صندوق', match: function (p, t) { return p === 'admin' && t === 'tab-manage'; } },
    { href: 'admin.html?tab=tab-installments-report', icon: 'fa-calendar-exclamation', label: 'قسط‌های نزدیک', match: function (p, t) { return p === 'admin' && t === 'tab-installments-report'; } },
    { href: 'admin.html?tab=tab-loans', icon: 'fa-file-invoice', label: 'همه وام‌ها', match: function (p, t) { return p === 'admin' && t === 'tab-loans'; } },
    { href: 'admin.html?tab=tab-past-loan', icon: 'fa-history', label: 'وام قبلی', match: function (p, t) { return p === 'admin' && t === 'tab-past-loan'; } },
    { href: 'admin.html?tab=tab-members', icon: 'fa-users', label: 'اعضا', match: function (p, t) { return p === 'admin' && t === 'tab-members'; } },
    { heading: 'پول صندوق' },
    { href: 'admin.html?tab=tab-fund-deposit', icon: 'fa-piggy-bank', label: 'ریختن پول', match: function (p, t) { return p === 'admin' && t === 'tab-fund-deposit'; } },
    { href: 'admin.html?tab=tab-deposits', icon: 'fa-money-check-alt', label: 'واریزی‌ها', match: function (p, t) { return p === 'admin' && t === 'tab-deposits'; } },
    { href: 'admin.html?tab=tab-ledger', icon: 'fa-book', label: 'حساب صندوق', match: function (p, t) { return p === 'admin' && t === 'tab-ledger'; } },
    { heading: 'سایر' },
    { href: 'admin.html?tab=tab-reminders', icon: 'fa-bell', label: 'پیام‌های بله', match: function (p, t) { return p === 'admin' && t === 'tab-reminders'; } },
    { href: 'admin.html?tab=tab-settings', icon: 'fa-sliders-h', label: 'تنظیمات', match: function (p, t) { return p === 'admin' && t === 'tab-settings'; } },
    { href: 'reports.html', icon: 'fa-chart-pie', label: 'گزارش مدیریتی', match: function (p) { return p === 'reports'; } },
    { href: 'roles.html', icon: 'fa-users-cog', label: 'نقش\u200cها', match: function (p) { return p === 'roles'; } }
  ];

  function renderAdminNav(containerId, options) {
    options = options || {};
    var page = options.page || '';
    var tab = options.tab || new URLSearchParams(location.search).get('tab') || (page === 'admin' ? 'tab-queue' : '');
    var container = $(containerId);
    if (!container) return;

    var html = '<button type="button" class="admin-nav-toggle" aria-expanded="false"><i class="fas fa-bars"></i><span>منوی مدیر</span></button>';
    html += '<button type="button" class="admin-nav-backdrop" aria-label="بستن منو"></button>';
    html += '<nav class="admin-nav" aria-label="منوی مدیر">';
    html += '<div class="admin-nav-title"><span>منوی مدیر</span><button type="button" class="admin-nav-close" aria-label="بستن"><i class="fas fa-times"></i></button></div>';
    ADMIN_NAV_ITEMS.forEach(function (item) {
      if (item.heading) {
        html += '<div class="admin-nav-heading">' + item.heading + '</div>';
        return;
      }
      var active = item.match(page, tab) ? ' active' : '';
      html += '<a href="' + item.href + '" class="' + active.trim() + '"><i class="fas ' + item.icon + '"></i><span>' + item.label + '</span></a>';
    });
    html += '</nav>';
    container.innerHTML = html;

    function setOpen(open) {
      container.classList.toggle('is-open', open);
      document.body.classList.toggle('admin-nav-open', open);
      var toggle = container.querySelector('.admin-nav-toggle');
      if (toggle) toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    container.querySelector('.admin-nav-toggle').addEventListener('click', function () {
      setOpen(!container.classList.contains('is-open'));
    });
    container.querySelector('.admin-nav-backdrop').addEventListener('click', function () { setOpen(false); });
    container.querySelector('.admin-nav-close').addEventListener('click', function () { setOpen(false); });
    container.querySelector('.admin-nav').addEventListener('click', function (e) {
      if (e.target.closest('a') && window.matchMedia('(max-width: 1099px)').matches) setOpen(false);
    });
  }

  function initStatsCollapse(selector) {
    var nodes = typeof selector === 'string'
      ? document.querySelectorAll(selector)
      : (selector ? [selector] : []);
    if (!nodes.length) return;

    var mq = window.matchMedia('(max-width: 768px)');
    function apply(el) {
      if (!el || el.tagName !== 'DETAILS') return;
      el.open = !mq.matches;
      var hint = el.querySelector('.stats-collapse-hint');
      if (hint) {
        hint.innerHTML = el.open
          ? 'برای بستن بزنید <i class="fas fa-chevron-down stats-collapse-chevron"></i>'
          : 'برای مشاهده آمار بزنید <i class="fas fa-chevron-down stats-collapse-chevron"></i>';
      }
    }

    Array.prototype.forEach.call(nodes, function (el) {
      apply(el);
      el.addEventListener('toggle', function () {
        if (!mq.matches) return;
        var hint = el.querySelector('.stats-collapse-hint');
        if (!hint) return;
        hint.innerHTML = el.open
          ? 'برای بستن بزنید <i class="fas fa-chevron-down stats-collapse-chevron"></i>'
          : 'برای مشاهده آمار بزنید <i class="fas fa-chevron-down stats-collapse-chevron"></i>';
      });
    });

    function onChange() {
      Array.prototype.forEach.call(nodes, apply);
    }
    if (mq.addEventListener) mq.addEventListener('change', onChange);
    else if (mq.addListener) mq.addListener(onChange);
  }

  function logout() {
    ['gl_userId', 'gl_fullName', 'gl_roleId', 'gl_roleName', 'gl_userStatus', 'gl_nationalCode', 'gl_entry'].forEach(function (key) {
      localStorage.removeItem(key);
    });
    location.href = 'index.html';
  }

  function getMobileNavItems() {
    var loggedIn = getUserId() > 0;
    var items = [];
    if (loggedIn) {
      items.push({ href: 'panel.html', icon: 'fa-th-large', label: 'پنل عضو', match: ['panel.html'] });
      items.push({ href: 'profile.html', icon: 'fa-id-card', label: 'پروفایل', match: ['profile.html'] });
      items.push({ href: 'index.html', icon: 'fa-home', label: 'خانه', match: ['index.html', ''] });
      items.push({ action: 'logout', icon: 'fa-sign-out-alt', label: 'خروج', match: [] });
    } else {
      items.push({ href: 'index.html', icon: 'fa-home', label: 'خانه', match: ['index.html', ''] });
      items.push({ href: 'login.html', icon: 'fa-sign-in-alt', label: 'ورود', match: ['login.html', 'register.html'] });
      items.push({ href: 'panel.html', icon: 'fa-th-large', label: 'پنل عضو', match: ['panel.html'] });
      items.push({ href: 'founders.html', icon: 'fa-users', label: 'بنیان‌گذاران', match: ['founders.html'] });
    }
    return items;
  }

  function renderMobileNav() {
    if (window.matchMedia('(min-width: 769px)').matches) return;

    var page = (location.pathname.split('/').pop() || 'index.html').toLowerCase();
    // صفحات تمام‌صفحه بدون منوی پایین (مخصوصاً WebView بله)
    if (page === 'admin.html' || page === 'reports.html' || page === 'roles.html') return;
    if (page === 'login.html' || page === 'register.html') return;
    if (page === 'pay-installment.html' || page === 'payment-result.html') return;

    document.body.classList.add('has-mobile-nav');

    var existing = document.querySelector('.mobile-bottom-nav');
    if (existing) existing.remove();

    var nav = document.createElement('nav');
    nav.className = 'mobile-bottom-nav';
    nav.setAttribute('aria-label', 'منوی اصلی موبایل');
    nav.innerHTML = getMobileNavItems().map(function (item) {
      var active = item.match.indexOf(page) >= 0 ? ' active' : '';
      if (item.action === 'logout') {
        return '<button type="button" class="mobile-nav-logout' + active + '" data-action="logout"><i class="fas ' + item.icon + '"></i><span>' + item.label + '</span></button>';
      }
      return '<a href="' + item.href + '" class="' + active.trim() + '"><i class="fas ' + item.icon + '"></i><span>' + item.label + '</span></a>';
    }).join('');

    nav.addEventListener('click', function (e) {
      var btn = e.target.closest('[data-action="logout"]');
      if (btn) {
        e.preventDefault();
        logout();
      }
    });

    document.body.appendChild(nav);
  }

  function scrollActiveTabIntoView() {
    var activeBtn = document.querySelector('.tab-menu .tab-btn.active');
    if (activeBtn && activeBtn.scrollIntoView) {
      activeBtn.scrollIntoView({ inline: 'center', block: 'nearest', behavior: 'smooth' });
    }
  }

  async function applyUserMenus(containerSelector) {
    const container = document.querySelector(containerSelector);
    if (!container) return;

    const userId = getUserId();
    if (!userId) return;

    const result = await get('/api/roles/my-menus/' + userId);
    const menus = payload(result) || [];
    if (!menus.length) return;

    const allowed = menus.map(function (m) {
      return (pick(m, 'selectorID') || pick(m, 'selectorId') || '').toLowerCase();
    });

    container.querySelectorAll('[data-menu]').forEach(function (link) {
      const key = (link.getAttribute('data-menu') || link.getAttribute('href') || '').toLowerCase();
      const ok = allowed.some(function (a) { return a === key || key.indexOf(a) >= 0; });
      link.style.display = ok ? '' : 'none';
    });
  }

  function resolveTable(target) {
    if (!target) return null;
    const el = typeof target === 'string' ? $(target.replace(/^#/, '')) || document.querySelector(target) : target;
    if (!el) return null;
    if (el.tagName === 'TABLE') return el;
    if (el.tagName === 'TBODY') return el.closest('table');
    return el.querySelector ? el.querySelector('table') : null;
  }

  function tableTitle(table) {
    const card = table.closest('.card, .tab-panel');
    const heading = card && card.querySelector('h2, h3');
    return heading ? heading.textContent.replace(/\s+/g, ' ').trim() : 'گزارش';
  }

  function cellText(td) {
    const clone = td.cloneNode(true);
    clone.querySelectorAll('button, .btn, .actions').forEach(function (n) { n.remove(); });
    return (clone.textContent || '').replace(/\s+/g, ' ').trim();
  }

  function parseSortValue(text) {
    const cleaned = toEnglishDigits(text).replace(/[^\d.-]/g, '');
    if (cleaned && !isNaN(Number(cleaned))) return Number(cleaned);
    return text;
  }

  function filterGlTable(table, query) {
    const q = (query || '').trim().toLowerCase();
    table.querySelectorAll('tbody tr').forEach(function (tr) {
      const text = tr.textContent.replace(/\s+/g, ' ').toLowerCase();
      tr.style.display = !q || text.indexOf(q) >= 0 ? '' : 'none';
    });
  }

  function sortGlTable(table, colIndex, dir) {
    const tbody = table.querySelector('tbody');
    if (!tbody) return;
    const rows = Array.from(tbody.querySelectorAll('tr'));
    rows.sort(function (a, b) {
      const av = parseSortValue(cellText(a.children[colIndex] || { textContent: '' }));
      const bv = parseSortValue(cellText(b.children[colIndex] || { textContent: '' }));
      if (typeof av === 'number' && typeof bv === 'number') return dir === 'asc' ? av - bv : bv - av;
      return dir === 'asc'
        ? String(av).localeCompare(String(bv), 'fa')
        : String(bv).localeCompare(String(av), 'fa');
    });
    rows.forEach(function (r) { tbody.appendChild(r); });
    table.dataset.glSortCol = String(colIndex);
    table.dataset.glSortDir = dir;
    table.querySelectorAll('thead th').forEach(function (th, i) {
      th.classList.toggle('sorted-asc', i === colIndex && dir === 'asc');
      th.classList.toggle('sorted-desc', i === colIndex && dir === 'desc');
    });
  }

  function printGlTable(table) {
    const title = tableTitle(table);
    const clone = table.cloneNode(true);
    clone.querySelectorAll('button, .btn, .actions').forEach(function (n) {
      const td = n.closest('td');
      if (td && td.querySelector('.btn')) td.textContent = '—';
      else n.remove();
    });
    clone.querySelectorAll('tbody tr').forEach(function (tr) {
      if (tr.style.display === 'none') tr.remove();
    });
    const w = window.open('', '_blank');
    if (!w) return;
    w.document.write('<html lang="fa" dir="rtl"><head><meta charset="UTF-8"><title>' + title + '</title>');
    w.document.write('<style>body{font-family:Tahoma,sans-serif;padding:1rem}table{width:100%;border-collapse:collapse}th,td{border:1px solid #ccc;padding:8px;text-align:right}th{background:#eef6d8}</style></head><body>');
    w.document.write('<h2>' + title + '</h2>' + clone.outerHTML + '</body></html>');
    w.document.close();
    w.focus();
    w.print();
  }

  function exportGlTableExcel(table) {
    const title = tableTitle(table);
    const lines = [];
    const headers = Array.from(table.querySelectorAll('thead th')).map(function (th) {
      return '"' + th.textContent.replace(/"/g, '""').trim() + '"';
    });
    lines.push(headers.join(','));
    table.querySelectorAll('tbody tr').forEach(function (tr) {
      if (tr.style.display === 'none') return;
      const cells = Array.from(tr.querySelectorAll('td')).map(function (td) {
        return '"' + cellText(td).replace(/"/g, '""') + '"';
      });
      if (cells.length) lines.push(cells.join(','));
    });
    const blob = new Blob(['\ufeff' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = (title || 'export').replace(/[\\/:*?"<>|]/g, '_') + '.csv';
    link.click();
    URL.revokeObjectURL(link.href);
  }

  function applyMobileTableLabels(table) {
    if (!table) return;
    var headers = Array.from(table.querySelectorAll('thead th')).map(function (th) {
      return th.textContent.replace(/[⇅↑↓]/g, '').trim();
    });
    table.querySelectorAll('tbody tr').forEach(function (tr) {
      tr.querySelectorAll('td').forEach(function (td, index) {
        if (td.colSpan > 1) {
          td.removeAttribute('data-label');
          return;
        }
        if (headers[index]) td.setAttribute('data-label', headers[index]);
      });
    });
  }

  function enhanceDataTable(target) {
    const table = resolveTable(target);
    if (!table || table.dataset.glEnhanced === '1') return table;

    table.dataset.glEnhanced = '1';
    table.classList.add('gl-data-table');

    const host = table.parentElement;
    const wrap = document.createElement('div');
    wrap.className = 'gl-table-wrap';
    host.insertBefore(wrap, table);

    const toolbar = document.createElement('div');
    toolbar.className = 'gl-table-toolbar';
    toolbar.innerHTML =
      '<div class="gl-table-search-wrap"><i class="fas fa-search"></i><input type="search" class="gl-table-search" placeholder="جستجو در جدول..." /></div>' +
      '<div class="gl-table-actions">' +
      '<button type="button" class="btn dark gl-table-print"><i class="fas fa-print"></i>چاپ</button>' +
      '<button type="button" class="btn primary gl-table-excel"><i class="fas fa-file-excel"></i>اکسل</button>' +
      '</div>';

    const scroll = document.createElement('div');
    scroll.className = 'gl-table-scroll';
    wrap.appendChild(toolbar);
    wrap.appendChild(scroll);
    scroll.appendChild(table);

    const searchInput = toolbar.querySelector('.gl-table-search');
    searchInput.addEventListener('input', function () { filterGlTable(table, searchInput.value); });
    toolbar.querySelector('.gl-table-print').addEventListener('click', function () { printGlTable(table); });
    toolbar.querySelector('.gl-table-excel').addEventListener('click', function () { exportGlTableExcel(table); });

    table.querySelectorAll('thead th').forEach(function (th, index) {
      if (/عملیات/i.test(th.textContent)) return;
      th.classList.add('sortable');
      th.addEventListener('click', function () {
        const dir = table.dataset.glSortCol === String(index) && table.dataset.glSortDir === 'asc' ? 'desc' : 'asc';
        sortGlTable(table, index, dir);
      });
    });

    applyMobileTableLabels(table);
    return table;
  }

  function initPageTables(root) {
    const scope = typeof root === 'string' ? document.querySelector(root) : (root || document);
    if (!scope) return;
    scope.querySelectorAll('table').forEach(function (table) {
      enhanceDataTable(table);
    });
  }

  function refreshDataTable(target) {
    const table = resolveTable(target);
    if (!table) return;
    if (table.dataset.glEnhanced !== '1') {
      enhanceDataTable(table);
      return;
    }
    const wrap = table.closest('.gl-table-wrap');
    const searchInput = wrap && wrap.querySelector('.gl-table-search');
    if (searchInput && searchInput.value) filterGlTable(table, searchInput.value);
    if (table.dataset.glSortCol != null) {
      sortGlTable(table, Number(table.dataset.glSortCol), table.dataset.glSortDir || 'asc');
    }
    applyMobileTableLabels(table);
  }



  return {

    $: $,

    ready: ready,

    showMsg: showMsg,
    confirmDialog: confirmDialog,

    money: money,
    formatNumber: formatNumber,
    parseAmount: parseAmount,
    shamsiDate: shamsiDate,
    loanStatusText: loanStatusText,
    renderLoanTableRow: renderLoanTableRow,
    setAmountValue: setAmountValue,
    getAmountValue: getAmountValue,
    bindAmountInputs: bindAmountInputs,

    getUserId: getUserId,

    setSession: setSession,

    requireLogin: requireLogin,
    requireManager: requireManager,
    isManagerRole: isManagerRole,
    isMultiRoleAccount: isMultiRoleAccount,
    panelIsDonor: panelIsDonor,
    applyEntry: applyEntry,
    mountEntrySwitcher: mountEntrySwitcher,
    normalizeNationalCode: normalizeNationalCode,

    post: post,

    put: put,

    get: get,
    del: del,
    request: request,

    openTab: openTab,

    initTabs: initTabs,

    bindClick: bindClick,

    bindFormEnter: bindFormEnter,

    pick: pick,

    payload: payload,

    normalizeApi: normalizeApi,
    applyUserMenus: applyUserMenus,
    renderAdminNav: renderAdminNav,
    renderMobileNav: renderMobileNav,
    initStatsCollapse: initStatsCollapse,
    logout: logout,
    toEnglishDigits: toEnglishDigits,
    initPageTables: initPageTables,
    refreshDataTable: refreshDataTable,
    enhanceDataTable: enhanceDataTable
  };

})();

GoodLoan.ready(function () {
  GoodLoan.renderMobileNav();
});


