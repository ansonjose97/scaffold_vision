import type { RecommendationRequest, RecommendationResponse } from '../domain/types';

const API_BASE = '/api';

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
  }
}

async function postJson<TReq, TRes>(path: string, body: TReq): Promise<TRes> {
  const response = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  });
  if (!response.ok) {
    const text = await response.text();
    throw new ApiError(response.status, text || response.statusText);
  }
  return response.json() as Promise<TRes>;
}

export const api = {
  recommend(request: RecommendationRequest): Promise<RecommendationResponse> {
    return postJson<RecommendationRequest, RecommendationResponse>('/recommendations', request);
  }
};
