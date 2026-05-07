import { Viewport } from './viewport/Viewport';
import { api, ApiError } from './api/client';
import { renderRecommendation } from './components/recommendation';
import type {
  BuildingDimensions,
  ScaffoldingPreferences,
  RecommendationResponse
} from './domain/types';

const $ = <T extends HTMLElement>(id: string): T => {
  const el = document.getElementById(id);
  if (!el) throw new Error(`Missing element: ${id}`);
  return el as T;
};

const viewportEl = $('viewport');
const statusEl = $('status');
const recommendationEl = $('recommendation-output');
const recommendBtn = $<HTMLButtonElement>('btn-recommend');

const viewport = new Viewport(viewportEl);

function readBuilding(): BuildingDimensions {
  return {
    widthMeters: parseFloat($<HTMLInputElement>('building-width').value),
    heightMeters: parseFloat($<HTMLInputElement>('building-height').value),
    depthMeters: parseFloat($<HTMLInputElement>('building-depth').value)
  };
}

function readPreferences(): ScaffoldingPreferences {
  return {
    bayWidthMeters: parseFloat($<HTMLInputElement>('bay-width').value),
    liftHeightMeters: parseFloat($<HTMLInputElement>('lift-height').value),
    braceEveryNBays: 5,
    wrapAround: $<HTMLInputElement>('wrap-around').checked
  };
}

function setStatus(text: string, kind: 'idle' | 'loading' | 'error' = 'idle'): void {
  statusEl.textContent = text;
  statusEl.dataset.kind = kind;
}

function syncBuildingPreview(): void {
  const building = readBuilding();
  if (Number.isFinite(building.widthMeters) &&
      Number.isFinite(building.heightMeters) &&
      Number.isFinite(building.depthMeters)) {
    viewport.updateBuilding(building);
  }
}

async function runRecommendation(): Promise<void> {
  const building = readBuilding();
  const preferences = readPreferences();

  recommendBtn.disabled = true;
  setStatus('Computing...', 'loading');

  try {
    const response: RecommendationResponse = await api.recommend({ building, preferences });
    renderRecommendation(recommendationEl, response);
    viewport.renderScaffold(building, preferences, response.summary);
    setStatus('Ready');
  } catch (err) {
    const message = err instanceof ApiError
      ? `Server error: ${err.message}`
      : 'Failed to reach the API. Is the backend running?';
    recommendationEl.innerHTML = `<p class="empty-state">${message}</p>`;
    setStatus('Error', 'error');
  } finally {
    recommendBtn.disabled = false;
  }
}

// Wire up events
['building-width', 'building-height', 'building-depth'].forEach((id) => {
  $(id).addEventListener('input', syncBuildingPreview);
});

recommendBtn.addEventListener('click', () => {
  void runRecommendation();
});

// Initial render
syncBuildingPreview();
setStatus('Ready');
