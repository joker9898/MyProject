// ============================================================
//  News_List.js  —  Filter + Pagination
// ============================================================
var totalPages    = 0;
var currentPage   = 1;

// Page culture ကို default lang အနေနဲ့ သုံးမည်
// newsAndEvents.cshtml ကနေ inject ဖြစ်လာသည်
var defaultLang   = (typeof currentCulture !== "undefined")
                    ? currentCulture
                    : "en-US";

var activeFilters = {
    topic:   "",
    country: "",
    from:    "",
    to:      "",
    lang:    defaultLang   // ← page culture ကို default ထားမည်
};

// Page load
FindNewsItems(currentPage);

// ── Filter Panel toggle ───────────────────────────────────
function toggleFilter() {
    var panel = document.getElementById("filter-panel");
    panel.style.display = (panel.style.display === "none") ? "block" : "none";
}

// ── Apply Filter ─────────────────────────────────────────
function applyFilter() {
    activeFilters.topic   = document.getElementById("filter-topic")?.value   || "";
    activeFilters.country = document.getElementById("filter-country")?.value || "";
    activeFilters.from    = document.getElementById("filter-from")?.value    || "";
    activeFilters.to      = document.getElementById("filter-to")?.value      || "";

    // Language မရွေးထားရင် page ရဲ့ default lang သုံးမည်
    var selectedLang = document.getElementById("filter-lang")?.value || "";
    activeFilters.lang = selectedLang || defaultLang;   // ← ဒါပြောင်းပါ

    currentPage = 1;
    FindNewsItems(currentPage);
    document.getElementById("filter-panel").style.display = "none";
}

// ── Reset Filter ─────────────────────────────────────────
function resetFilter() {
    document.getElementById("filter-topic").value   = "";
    document.getElementById("filter-country").value = "";
    document.getElementById("filter-from").value    = "";
    document.getElementById("filter-to").value      = "";
    document.getElementById("filter-lang").value    = "";

    activeFilters = { topic:"", country:"", from:"", to:"", lang:"" };
    currentPage   = 1;
    FindNewsItems(currentPage);
    document.getElementById("filter-panel").style.display = "none";
}

// ── Main Fetch Function ──────────────────────────────────
function FindNewsItems(page) {

    // Query string ဆောက်သည်
    const params = new URLSearchParams();
    params.append("page", page);
    if (activeFilters.topic)   params.append("topic",   activeFilters.topic);
    if (activeFilters.country) params.append("country", activeFilters.country);
    if (activeFilters.from)    params.append("from",    activeFilters.from);
    if (activeFilters.to)      params.append("to",      activeFilters.to);
    if (activeFilters.lang)    params.append("lang",    activeFilters.lang);

    fetch(`/umbraco/api/infocontent/findnews?${params.toString()}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" }
    })
    .then(response => response.json())
    .then(data => {
        totalPages  = data.totalPages;
        currentPage = data.currentPage;

        var container = document.getElementById("news-result-container");
        if (!container) return;
        container.innerHTML = "";

        if (data.newsItemData && data.newsItemData.length > 0) {
            data.newsItemData.forEach(item => {
                const card = document.createElement("div");
                card.className = "home-card";
                card.innerHTML = `
                    <a href="${item.url}" class="home-card-img-wrap">
                        ${item.imageResourceUrl
                            ? `<img src="${item.imageResourceUrl}"
                                    alt="${item.titleNews}"
                                    class="home-card-img" />`
                            : `<div class="home-card-img-placeholder">
                                   <i class="fa fa-newspaper"></i>
                               </div>`
                        }
                    </a>
                    <div class="home-card-body">
                        <h3 class="home-card-title">
                            <a href="${item.url}">${item.titleNews}</a>
                        </h3>
                        <p class="home-card-desc">
                            ${item.description
                                ? item.description.replace(/<[^>]*>?/gm, "").substring(0, 120) + "..."
                                : ""}
                        </p>
                        <div class="d-flex justify-content-between align-items-center mt-2">
                            <a href="${item.url}" class="home-card-link">READ MORE →</a>
                            <small class="text-muted">${item.lastUpdate}</small>
                        </div>
                    </div>
                `;
                container.appendChild(card);
            });
        } else {
            container.innerHTML = "<p class='home-empty'>No news found.</p>";
        }

        renderPagination(totalPages, currentPage);
    })
    .catch(err => console.error("News List Error:", err));
}

// ── Pagination ───────────────────────────────────────────
function renderPagination(totalPages, currentPage) {
    var el = document.getElementById("news-pagination");
    if (!el) return;
    el.innerHTML = "";
    if (totalPages <= 1) return;

    const prev     = document.createElement("button");
    prev.innerText  = "← Prev";
    prev.className  = "btn btn-outline-danger btn-sm me-1";
    prev.disabled   = currentPage === 1;
    prev.onclick    = () => FindNewsItems(currentPage - 1);
    el.appendChild(prev);

    for (let i = 1; i <= totalPages; i++) {
        const btn    = document.createElement("button");
        btn.innerText = i;
        btn.className = i === currentPage
            ? "btn btn-danger btn-sm me-1"
            : "btn btn-outline-danger btn-sm me-1";
        btn.onclick = () => FindNewsItems(i);
        el.appendChild(btn);
    }

    const next     = document.createElement("button");
    next.innerText  = "Next →";
    next.className  = "btn btn-outline-danger btn-sm";
    next.disabled   = currentPage === totalPages;
    next.onclick    = () => FindNewsItems(currentPage + 1);
    el.appendChild(next);
}
