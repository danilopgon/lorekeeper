import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Campaign, CampaignDto } from '../models/campaign.model';
import { CampaignApiError, mapProblemDetails, ProblemDetails } from '../state/problem-details';

/**
 * Handwritten Block 01 adapter for the backend campaign HTTP contract.
 *
 * Generated-client automation intentionally starts outside this slice.
 */
@Injectable({ providedIn: 'root' })
export class CampaignsApiService {
  private readonly http = inject(HttpClient);

  /** Lists campaigns from the backend contract. */
  async listCampaigns(): Promise<Campaign[]> {
    try {
      const campaigns = await firstValueFrom(this.http.get<unknown>('/api/campaigns'));
      return parseCampaignArray(campaigns);
    } catch (error) {
      throw toCampaignApiError(error);
    }
  }

  /** Creates one campaign by name. */
  async createCampaign(name: string): Promise<Campaign> {
    try {
      const campaign = await firstValueFrom(this.http.post<unknown>('/api/campaigns', { name }));
      return parseCampaign(campaign);
    } catch (error) {
      throw toCampaignApiError(error);
    }
  }

  /** Gets one campaign by canonical campaign id. */
  async getCampaign(id: string): Promise<Campaign> {
    try {
      const campaign = await firstValueFrom(
        this.http.get<unknown>(`/api/campaigns/${encodeURIComponent(id)}`),
      );
      return parseCampaign(campaign);
    } catch (error) {
      throw toCampaignApiError(error);
    }
  }
}

function parseCampaignArray(value: unknown): Campaign[] {
  if (!Array.isArray(value)) {
    throw { kind: 'recoverableError', code: 'unknown_error' } satisfies CampaignApiError;
  }

  return value.map(parseCampaign);
}

function parseCampaign(value: unknown): Campaign {
  if (!isCampaignDto(value)) {
    throw { kind: 'recoverableError', code: 'unknown_error' } satisfies CampaignApiError;
  }

  return toCampaign(value);
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
  if (isCampaignApiError(error)) {
    return error;
  }

  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return { kind: 'recoverableError', code: 'network_error' };
    }

    return mapProblemDetails(isProblemDetails(error.error) ? error.error : undefined);
  }

  return { kind: 'recoverableError', code: 'unknown_error' };
}

function isCampaignDto(value: unknown): value is CampaignDto {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const candidate = value as Partial<Record<keyof CampaignDto, unknown>>;
  return (
    typeof candidate.id === 'string' &&
    typeof candidate.name === 'string' &&
    typeof candidate.createdAt === 'string' &&
    typeof candidate.updatedAt === 'string'
  );
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null;
}

function isCampaignApiError(value: unknown): value is CampaignApiError {
  return typeof value === 'object' && value !== null && 'kind' in value && 'code' in value;
}
