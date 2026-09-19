/** Campaign model consumed by the Angular campaign feature. */
export interface Campaign {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
}

/** Backend campaign DTO shape for the Block 01 HTTP contract. */
export interface CampaignDto {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
}
