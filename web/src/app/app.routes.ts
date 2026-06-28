import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { ClientsComponent } from './clients/clients.component';
import { ReceiptsComponent } from './receipts/receipts.component';
import { ReportComponent } from './report/report.component';
import { UsersComponent } from './users/users.component';
import { ProfileComponent } from './profile/profile.component';
import { authGuard, adminGuard } from './auth.guard';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'clients', component: ClientsComponent, canActivate: [authGuard] },
    { path: 'receipts', component: ReceiptsComponent, canActivate: [authGuard] },
    { path: 'report', component: ReportComponent, canActivate: [authGuard] },
    { path: 'users', component: UsersComponent, canActivate: [adminGuard] },
    { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
    { path: '', redirectTo: '/login', pathMatch: 'full' },
    { path: '**', redirectTo: '/login' }
];
