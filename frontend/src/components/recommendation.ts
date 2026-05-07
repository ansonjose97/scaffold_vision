import type { RecommendationResponse } from '../domain/types';

const eur = new Intl.NumberFormat('de-DE', {
  style: 'currency',
  currency: 'EUR',
  maximumFractionDigits: 0
});

const num = new Intl.NumberFormat('de-DE');

export function renderRecommendation(
  container: HTMLElement,
  response: RecommendationResponse
): void {
  const summaryHtml = `
    <div class="summary-grid">
      <div class="summary-card">
        <div class="summary-card-label">Bays</div>
        <div class="summary-card-value">${response.summary.bays}</div>
      </div>
      <div class="summary-card">
        <div class="summary-card-label">Lifts</div>
        <div class="summary-card-value">${response.summary.lifts}</div>
      </div>
      <div class="summary-card">
        <div class="summary-card-label">Facade</div>
        <div class="summary-card-value">
          ${response.summary.linearFacadeMeters.toFixed(1)}<span class="summary-card-unit">m</span>
        </div>
      </div>
      <div class="summary-card">
        <div class="summary-card-label">Area</div>
        <div class="summary-card-value">
          ${response.summary.totalAreaSquareMeters.toFixed(0)}<span class="summary-card-unit">m²</span>
        </div>
      </div>
    </div>
  `;

  const itemsHtml = response.components
    .map(
      (c) => `
        <div class="line-item">
          <span class="line-item-name">${c.name}</span>
          <span class="line-item-qty">×${num.format(c.quantity)}</span>
          <span class="line-item-total">${eur.format(c.lineTotalEur)}</span>
        </div>
      `
    )
    .join('');

  const totalsHtml = `
    <div class="totals">
      <div class="totals-row">
        <span>Estimated weight</span>
        <span>${num.format(Math.round(response.estimatedWeightKg))} kg</span>
      </div>
      <div class="totals-row">
        <span>Total</span>
        <span>${eur.format(response.estimatedTotalEur)}</span>
      </div>
    </div>
  `;

  const notesHtml =
    response.notes.length > 0
      ? `
        <div class="notes">
          <div class="notes-title">Notes</div>
          <ul>
            ${response.notes.map((n) => `<li>${escapeHtml(n)}</li>`).join('')}
          </ul>
        </div>
      `
      : '';

  container.innerHTML = `
    ${summaryHtml}
    <div class="line-items">${itemsHtml}</div>
    ${totalsHtml}
    ${notesHtml}
  `;
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}
