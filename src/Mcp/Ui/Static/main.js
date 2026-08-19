import { App } from "@modelcontextprotocol/ext-apps";

const app = new App({ name: "RaindropExplorer", version: "1.0.0" });

app.ontoolresult = (result) => {
    const statusEl = document.getElementById("status");
    const containerEl = document.getElementById("bookmarks-container");

    if (result && result.structuredContent) {
        statusEl.style.display = "none";
        containerEl.style.display = "grid";

        renderBookmarks(result.structuredContent);
    } else {
        statusEl.innerText = "No bookmark data received in tool result.";
    }
};

app.ontoolcancelled = () => {
    const statusEl = document.getElementById("status");
    statusEl.style.display = "block";
    statusEl.textContent = "Bookmark loading was cancelled.";
};

async function loadDetails(id, button) {
    const detailsBlock = document.getElementById(`details-${id}`);
    button.disabled = true;
    button.textContent = "Loading…";
    detailsBlock.textContent = "Loading bookmark details…";
    detailsBlock.style.display = "block";

    try {
        const response = await app.callServerTool({ name: "fetch_bookmark_details", arguments: { bookmarkId: id } });
        if (response?.isError) {
            detailsBlock.textContent = response.content?.[0]?.text || "Failed to fetch bookmark details.";
        } else if (response?.structuredContent) {
            detailsBlock.innerText = JSON.stringify(response.structuredContent, null, 2);
        } else {
            detailsBlock.textContent = "No bookmark details were returned.";
        }
    } catch (err) {
        console.error("Error loading details:", err);
        detailsBlock.textContent = `Unable to load details: ${err instanceof Error ? err.message : "Unknown error"}`;
    } finally {
        button.disabled = false;
        button.textContent = "Load Details";
    }
}

function renderBookmarks(structuredContent) {
    const container = document.getElementById("bookmarks-container");
    container.replaceChildren();

    let bookmarks = structuredContent;
    // Handle both direct array and wrapped pagination objects
    if (structuredContent && structuredContent.items) {
        bookmarks = structuredContent.items;
    }

    if (!Array.isArray(bookmarks) || bookmarks.length === 0) {
        const empty = document.createElement("p");
        empty.textContent = "No bookmarks to display.";
        container.appendChild(empty);
        return;
    }

    bookmarks.forEach(bookmark => {
        const article = document.createElement("article");
        article.className = "bookmark-card";

        const contentDiv = document.createElement("div");

        const header = document.createElement("header");

        // Placeholder image strategy
        const coverDiv = document.createElement("div");
        coverDiv.className = "cover-placeholder";
        if (bookmark.domain) {
            coverDiv.innerText = bookmark.domain.substring(0, 2);
        } else if (bookmark.title) {
            coverDiv.innerText = bookmark.title.substring(0, 2);
        } else {
            coverDiv.innerText = "??";
        }
        header.appendChild(coverDiv);

        const titleDiv = document.createElement("div");
        titleDiv.className = "bookmark-title";

        const titleLink = document.createElement("a");
        titleLink.href = (bookmark.link && (bookmark.link.startsWith("http://") || bookmark.link.startsWith("https://"))) ? bookmark.link : "#";
        titleLink.target = "_blank";
        titleLink.rel = "noopener noreferrer";
        // safe DOM text insertion
        titleLink.textContent = bookmark.title || "Untitled";

        titleDiv.appendChild(titleLink);
        header.appendChild(titleDiv);

        contentDiv.appendChild(header);

        const excerptP = document.createElement("p");
        excerptP.className = "bookmark-excerpt";
        excerptP.textContent = bookmark.excerpt || "";
        contentDiv.appendChild(excerptP);

        article.appendChild(contentDiv);

        const footer = document.createElement("footer");
        footer.className = "bookmark-footer";

        const visitLink = document.createElement("a");
        visitLink.href = (bookmark.link && (bookmark.link.startsWith("http://") || bookmark.link.startsWith("https://"))) ? bookmark.link : "#";
        visitLink.target = "_blank";
        visitLink.rel = "noopener noreferrer";
        visitLink.textContent = "Visit Link";
        footer.appendChild(visitLink);

        if (bookmark.domain) {
            const domainSpan = document.createElement("span");
            domainSpan.textContent = ` • ${bookmark.domain}`;
            footer.appendChild(domainSpan);
        }

        const detailsBtn = document.createElement("button");
        detailsBtn.className = "outline";
        detailsBtn.textContent = "Load Details";
        detailsBtn.addEventListener("click", () => loadDetails(bookmark.id, detailsBtn));
        footer.appendChild(detailsBtn);

        article.appendChild(footer);

        const detailsPre = document.createElement("pre");
        detailsPre.id = `details-${bookmark.id}`;
        detailsPre.style.display = "none";
        article.appendChild(detailsPre);

        container.appendChild(article);
    });
}

// Connect after setting handlers
app.connect().catch(err => {
    const statusEl = document.getElementById("status");
    statusEl.innerText = `Failed to connect: ${err.message}`;
    console.error("Connection error:", err);
});
