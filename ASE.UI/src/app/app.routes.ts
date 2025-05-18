import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { DealersComponent } from './features/dealers/dealers.component';
import { DealerDetailsComponent } from './features/dealers/dealer-details.component';
import { DealerPatternsComponent } from './features/dealers/dealer-patterns.component';
import { DealerAnomaliesComponent } from './features/dealers/dealer-anomalies.component';
import { FinanceSubmissionsComponent } from './features/finance-submissions/finance-submissions.component';
import { FinanceSubmissionDetailsComponent } from './features/finance-submissions/finance-submission-details.component';
import { MasterTemplatesComponent } from './features/master-templates/master-templates.component';
import { QueryBuilderComponent } from './features/query-builder/query-builder.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'dealers', component: DealersComponent },
  { path: 'dealers/new', component: DealerDetailsComponent },
  { path: 'dealers/:id', component: DealerDetailsComponent },
  { path: 'dealers/:id/patterns', component: DealerPatternsComponent },
  { path: 'dealers/:id/anomalies', component: DealerAnomaliesComponent },
  { path: 'dealer-groups/:groupId/patterns', component: DealerPatternsComponent },
  { path: 'dealer-groups/:groupId/anomalies', component: DealerAnomaliesComponent },
  { path: 'patterns', component: DealerPatternsComponent },
  { path: 'anomalies', component: DealerAnomaliesComponent },
  { path: 'submissions', component: FinanceSubmissionsComponent },
  { path: 'submissions/new', component: FinanceSubmissionDetailsComponent },
  { path: 'submissions/:id', component: FinanceSubmissionDetailsComponent },
  { path: 'templates', component: MasterTemplatesComponent },
  { path: 'query-builder', component: QueryBuilderComponent }
]; 