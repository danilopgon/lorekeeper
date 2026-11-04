import { Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../../shared/ui/page-header/page-header.component';
import { toCampaignApiError } from '../../api/campaign-api-error';
import { injectCampaignQuery } from '../../api/campaign.queries';
import { formatCampaignDate } from '../../models/campaign-date';
import { parseCampaignId } from '../../routing/campaign-id';

type WorkspaceKind = 'chat' | 'sources';

/** Route shell that loads campaign context before rendering unavailable workspace capabilities. */
@Component({
  imports: [PageHeaderComponent, RouterLink],
  selector: 'app-campaign-workspace-shell',
  templateUrl: './campaign-workspace-shell.component.html',
})
export class CampaignWorkspaceShellComponent {
  readonly workspaceKind = input.required<WorkspaceKind>();

  private readonly route = inject(ActivatedRoute);
  private readonly paramMap = toSignal(this.route.paramMap, {
    initialValue: this.route.snapshot.paramMap,
  });

  protected readonly campaignId = computed(() =>
    parseCampaignId(this.paramMap().get('campaignId')),
  );
  protected readonly validCampaignId = computed(() => {
    const result = this.campaignId();
    return result.valid ? result.value : undefined;
  });
  protected readonly campaign = injectCampaignQuery(this.validCampaignId);
  protected readonly currentCampaign = computed(() => {
    const expectedId = this.validCampaignId();

    if (!expectedId || !this.campaign.hasValue()) {
      return undefined;
    }

    const campaign = this.campaign.value();
    return campaign.id === expectedId ? campaign : undefined;
  });

  protected readonly sectionTitle = computed(() =>
    this.workspaceKind() === 'chat'
      ? 'El chat aún no está disponible'
      : 'Las fuentes aún no están disponibles',
  );
  protected readonly unavailableCopy = computed(() =>
    this.workspaceKind() === 'chat'
      ? 'Aquí preguntarás por esta campaña y verás de qué notas sale cada respuesta.'
      : 'Aquí añadirás notas en Markdown o texto y verás en qué estado está cada una.',
  );
  protected readonly formatDate = formatCampaignDate;
  protected readonly stateHeading = computed(() => {
    switch (this.errorKind()) {
      case 'invalid':
        return 'Enlace de campaña no válido';
      case 'notFound':
        return 'Campaña no encontrada';
      case 'recoverable':
        return 'No se pudo cargar la campaña';
      default:
        return 'Cargando campaña…';
    }
  });

  protected errorKind(): 'invalid' | 'notFound' | 'recoverable' | null {
    if (!this.campaignId().valid) {
      return 'invalid';
    }

    const error = this.campaign.error();

    if (!error) {
      return null;
    }

    return toCampaignApiError(error).kind === 'notFound' ? 'notFound' : 'recoverable';
  }
}
