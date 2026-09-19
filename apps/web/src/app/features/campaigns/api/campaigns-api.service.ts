import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Campaign, CampaignDto } from '../models/campaign.model';
import { CampaignApiError, mapProblemDetails, ProblemDetails } from '../state/problem-details';

// Block 01 keeps this adapter handwritten and narrow; generated-client automation starts outside this slice.
@Injectable({ providedIn: 'root' })
export class CampaignsApiService {
  private readonly http = inject(HttpClient);

  async listCampaigns(): Promise<Campaign[]> {
    try {
      const campaigns = await firstValueFrom(this.http.get<CampaignDto[]>('/api/campaigns'));
      return campaigns.map(toCampaign);
    } catch (error) {
      throw toCampaignApiError(error);
    }
  }

  async createCampaign(name: string): Promise<Campaign> {
    try {
      const campaign = await firstValueFrom(
        this.http.post<CampaignDto>('/api/campaigns', { name }),
      );
      return toCampaign(campaign);
    } catch (error) {
      throw toCampaignApiError(error);
    }
  }

  async getCampaign(id: string): Promise<Campaign> {
    try {
      const campaign = await firstValueFrom(
        this.http.get<CampaignDto>(`/api/campaigns/${encodeURIComponent(id)}`),
      );
      return toCampaign(campaign);
    } catch (error) {
      throw toCampaignApiError(error);
    }
  }
}

function toCampaign(dto: CampaignDto): Campaign {
  return {
    id: dto.id,
    name: dto.name,
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
  };
}

function toCampaignApiError(error: unknown): CampaignApiError {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return { kind: 'recoverableError', code: 'network_error' };
    }

    return mapProblemDetails(isProblemDetails(error.error) ? error.error : undefined);
  }

  return { kind: 'recoverableError', code: 'unknown_error' };
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null;
}
