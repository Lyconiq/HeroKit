// Turkish pages live under tr/ and use the Turkish navbar (tr/toc.yml).
// docfx reads this meta tag after the template module loads.
const isTurkish = /\/tr(\/|$)/.test(location.pathname);
if (isTurkish) {
  const rel = document.querySelector('meta[name="docfx:rel"]')?.content ?? '';
  document.querySelector('meta[name="docfx:navrel"]')?.setAttribute('content', `${rel}tr/toc.html`);
}

// Vercel Web Analytics: cookie-free page view counts, only on the production site served by Vercel.
if (location.hostname === 'herokit.lyconiq.com') {
  window.va = window.va || function () { (window.vaq = window.vaq || []).push(arguments); };
  const analytics = document.createElement('script');
  analytics.defer = true;
  analytics.src = '/_vercel/insights/script.js';
  document.head.appendChild(analytics);
}

export default {
  defaultTheme: 'auto',
  iconLinks: [
    { icon: 'github', href: 'https://github.com/Lyconiq/HeroKit', title: 'GitHub' },
    { icon: 'box-seam', href: 'https://www.nuget.org/packages/HeroKit', title: 'NuGet' },
  ],
  start: () => {
    // Copy buttons for the install commands on the landing page.
    document.querySelectorAll('[data-copy]').forEach(button => {
      button.addEventListener('click', async () => {
        await navigator.clipboard.writeText(button.dataset.copy);
        const label = button.textContent;
        button.textContent = isTurkish ? 'Kopyalandı' : 'Copied';
        setTimeout(() => (button.textContent = label), 1500);
      });
    });
  },
};
