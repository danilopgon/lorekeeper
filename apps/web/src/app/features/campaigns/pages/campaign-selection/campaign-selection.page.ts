import { Component, DestroyRef, computed, effect, inject, resource, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, from } from 'rxjs';
import { PageHeaderComponent } from '../../../../shared/ui/page-header/page-header.component';
import { CampaignsApiService } from '../../api/campaigns-api.service';
import { Campaign } from '../../models/campaign.model';
import { formatCampaignDate } from '../../models/campaign-date';
import { CampaignApiError } from '../../state/problem-details';

/** Campaign selection and creation route for the Block 01 workspace entry point. */
@Component({
  imports: [FormsModule, PageHeaderComponent, RouterLink],
  selector: 'app-campaign-selection-page',
  templateUrl: './campaign-selection.page.html',
})
export class CampaignSelectionPage {
  private readonly campaignsApi = inject(CampaignsApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refreshAfterActiveLoad = signal(false);

  protected campaignName = '';
  protected readonly creating = signal(false);
  protected readonly nameError = signal<'campaign_name_invalid' | 'campaign_name_conflict' | null>(
    null,
  );
  protected readonly createRecoverableError = signal(false);
  protected readonly campaigns = resource<Campaign[], unknown>({
    loader: () => this.campaignsApi.listCampaigns(),
  });
  protected readonly campaignCountFolio = computed(() => {
    const count = this.campaigns.hasValue() ? this.campaigns.value().length : 0;
    if (count === 0) {
      return undefined;
    }
    return count === 1 ? '1 campaña' : `${count} campañas`;
  });
  protected readonly formatDate = formatCampaignDate;

  constructor() {
    effect(() => {
      if (this.refreshAfterActiveLoad() && !this.campaigns.isLoading()) {
        this.refreshAfterActiveLoad.set(false);
        this.campaigns.reload();
      }
    });
  }

  /** Creates a campaign without selecting or navigating to it implicitly. */
  protected createCampaign(): void {
    if (this.creating()) {
      return;
    }

    const name = this.campaignName;
    this.startCreate();

    from(this.campaignsApi.createCampaign(name))
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.creating.set(false)),
      )
      .subscribe({
        next: () => this.handleCreateSuccess(),
        error: (error: unknown) => this.handleCreateError(error),
      });
  }

  /** Maps stable API field error codes to Spanish form copy. */
  protected fieldErrorMessage(code: 'campaign_name_invalid' | 'campaign_name_conflict'): string {
    return code === 'campaign_name_conflict'
      ? 'Ya existe una campaña con ese nombre.'
      : 'Escribe un nombre de entre 1 y 120 caracteres.';
  }

  private startCreate(): void {
    this.creating.set(true);
    this.nameError.set(null);
    this.createRecoverableError.set(false);
  }

  private handleCreateSuccess(): void {
    this.campaignName = '';

    if (this.campaigns.isLoading()) {
      this.refreshAfterActiveLoad.set(true);
      return;
    }

    this.campaigns.reload();
  }

  private handleCreateError(error: unknown): void {
    const apiError = error as Partial<CampaignApiError>;

    if (apiError.kind === 'fieldError' && apiError.field === 'name') {
      this.nameError.set(apiError.code as 'campaign_name_invalid' | 'campaign_name_conflict');
      return;
    }

    this.createRecoverableError.set(true);
  }
}
