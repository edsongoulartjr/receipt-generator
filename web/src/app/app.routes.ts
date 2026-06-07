import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { ClientsComponent } from './clients/clients.component';
import { ReceiptsComponent } from './receipts/receipts.component';
import { UsersComponent } from './users/users.component';
import { authGuard, superAdminGuard } from './auth.guard';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'clients', component: ClientsComponent, canActivate: [authGuard] },
    { path: 'receipts', component: ReceiptsComponent, canActivate: [authGuard] },
    { path: 'users', component: UsersComponent, canActivate: [superAdminGuard] },
    { path: '', redirectTo: '/login', pathMatch: 'full' },
    { path: '**', redirectTo: '/login' }
];
