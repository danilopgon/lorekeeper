import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CampaignDto, CreateCampaignRequest } from '../../../shared/api/generated';
import { zCreateCampaignResponse } from '../../../shared/api/generated/zod.gen';
import { toCampaignApiError } from './campaign-api-error';

/** Campaign write operations; failures reject with a `CampaignApiError`. */
@Injectable({ providedIn: 'root' })
export class CampaignCommands {
  private readonly http = inject(HttpClient);
  private readonly campaignsUrl = `${environment.apiBaseUrl}/api/campaigns`;

  /** Creates one campaign and returns its validated representation. */
  async createCampaign(request: CreateCampaignRequest): Promise<CampaignDto> {
    try {
      const campaign = await firstValueFrom(this.http.post<unknown>(this.campaignsUrl, request));
      return zCreateCampaignResponse.parse(campaign);
    } catch (error) {
      throw toCampaignApiError(error);
    }
  }
}
