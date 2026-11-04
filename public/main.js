// Turkish pages live under tr/ and use the Turkish navbar (tr/toc.yml).
// docfx reads this meta tag after the template module loads.
const isTurkish = /\/tr(\/|$)/.test(location.pathname);
if (isTurkish) {
  const rel = document.querySelector('meta[name="docfx:rel"]')?.content ?? '';
  document.querySelector('meta[name="docfx:navrel"]')?.setAttribute('content', `${rel}tr/toc.html`);
}

// Umami on analytics.lyconiq.com (our own server): cookie-free aggregate counts, only on the production site.
// Only utm_* survives from a URL, so a search term or tab choice never reaches the counts. The filter is set
// here, before the script is added: if this file fails to load, Umami does not load either.
if (location.hostname === 'herokit.lyconiq.com') {
  window.heroKitUmamiFilter = (type, payload) => {
    const clean = value => {
      if (!value) return value;
      const relative = value.charAt(0) === '/';
      const url = new URL(value, location.origin);
      const keep = new URLSearchParams();
      url.searchParams.forEach((v, k) => {
        if (/^utm_[a-z]+$/.test(k)) keep.append(k, v);
      });
      url.search = keep.toString();
      url.hash = '';
      return relative ? url.pathname + url.search : url.toString();
    };
    try {
      payload.url = clean(payload.url);
      payload.referrer = clean(payload.referrer);
      return payload;
    } catch {
      return null;
    }
  };
  const analytics = document.createElement('script');
  analytics.defer = true;
  analytics.src = 'https://analytics.lyconiq.com/s.js';
  analytics.dataset.websiteId = 'a02036f1-2d98-48a2-a468-3bbbcc88026d';
  analytics.dataset.excludeHash = 'true';
  analytics.dataset.beforeSend = 'heroKitUmamiFilter';
  document.head.appendChild(analytics);
}

// Measurement must never stop the site.
const track = name => {
  try {
    window.umami?.track(name);
  } catch {
    // Ignored on purpose.
  }
};

// The project's own GitHub pages count; "edit this page" and "view source" links do not.
const outboundEvent = link => {
  if (link.hostname === 'www.nuget.org' && /^\/packages\/HeroKit/i.test(link.pathname)) return 'nuget_clicked';
  if (link.hostname === 'github.com' && /^\/Lyconiq\/HeroKit(\/?$|\/(discussions|issues|releases))/i.test(link.pathname)) return 'github_clicked';
  return null;
};

const trackOutbound = event => {
  if (event.button > 1) return; // Left or middle click only.
  const link = event.target instanceof Element ? event.target.closest('a[href]') : null;
  const name = link && outboundEvent(link);
  if (name) track(name);
};

export default {
  defaultTheme: 'auto',
  iconLinks: [
    { icon: 'github', href: 'https://github.com/Lyconiq/HeroKit', title: 'GitHub' },
    { icon: 'box-seam', href: 'https://www.nuget.org/packages/HeroKit', title: 'NuGet' },
  ],
  start: () => {
    document.addEventListener('click', trackOutbound);
    document.addEventListener('auxclick', trackOutbound);

    // Copy buttons for the install commands on the landing page.
    document.querySelectorAll('[data-copy]').forEach(button => {
      button.addEventListener('click', async () => {
        await navigator.clipboard.writeText(button.dataset.copy);
        track('install_command_copied');
        const label = button.textContent;
        button.textContent = isTurkish ? 'Kopyalandı' : 'Copied';
        setTimeout(() => (button.textContent = label), 1500);
      });
    });
  },
};
