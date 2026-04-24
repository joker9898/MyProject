// ============================================================
//  News_List.js
//  /FindNews/{page} API ကနေ news ဆွဲပြီး card + pagination ပြ
// ============================================================

var totalPages  = 0;
var currentPage = 1;

// Page load ချိန်မှာ ပထမဆုံး page ဆွဲ
FindNewsItems(currentPage);

function FindNewsItems(page) {
    fetch(`/FindNews/${page}`, {
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

        // Cards ဖန်တီး
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

        // Pagination ဖန်တီး
        renderPagination(totalPages, currentPage);
    })
    .catch(err => console.error("News List Error:", err));
}

function renderPagination(totalPages, currentPage) {
    var el = document.getElementById("news-pagination");
    if (!el) return;
    el.innerHTML = "";
    if (totalPages <= 1) return;

    // Prev ခလုတ်
    const prev    = document.createElement("button");
    prev.innerText = "← Prev";
    prev.className = "btn btn-outline-danger btn-sm me-1";
    prev.disabled  = currentPage === 1;
    prev.onclick   = () => FindNewsItems(currentPage - 1);
    el.appendChild(prev);

    // Page numbers
    for (let i = 1; i <= totalPages; i++) {
        const btn     = document.createElement("button");
        btn.innerText  = i;
        btn.className  = i === currentPage
            ? "btn btn-danger btn-sm me-1"
            : "btn btn-outline-danger btn-sm me-1";
        btn.onclick = () => FindNewsItems(i);
        el.appendChild(btn);
    }

    // Next ခလုတ်
    const next    = document.createElement("button");
    next.innerText = "Next →";
    next.className = "btn btn-outline-danger btn-sm";
    next.disabled  = currentPage === totalPages;
    next.onclick   = () => FindNewsItems(currentPage + 1);
    el.appendChild(next);
}