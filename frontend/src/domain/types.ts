// Type definitions mirroring the backend's contracts.
// Kept manually in sync with the C# records in ScaffoldVision.Api.Models.

export interface BuildingDimensions {
  widthMeters: number;
  heightMeters: number;
  depthMeters: number;
}

export interface ScaffoldingPreferences {
  bayWidthMeters: number;
  liftHeightMeters: number;
  braceEveryNBays: number;
  wrapAround: boolean;
}

export interface RecommendationRequest {
  building: BuildingDimensions;
  preferences?: ScaffoldingPreferences;
}

export interface RecommendationSummary {
  bays: number;
  lifts: number;
  linearFacadeMeters: number;
  totalAreaSquareMeters: number;
}

export interface RecommendedComponent {
  sku: string;
  name: string;
  quantity: number;
  lineTotalEur: number;
}

export interface RecommendationResponse {
  summary: RecommendationSummary;
  components: RecommendedComponent[];
  notes: string[];
  estimatedTotalEur: number;
  estimatedWeightKg: number;
}
