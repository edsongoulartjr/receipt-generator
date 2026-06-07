import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { ClientsComponent } from './clients/clients.component';
import { ReceiptsComponent } from './receipts/receipts.component';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'register', component: RegisterComponent },
    { path: 'clients', component: ClientsComponent },
    { path: 'receipts', component: ReceiptsComponent },
    { path: '', redirectTo: '/login', pathMatch: 'full' },
    { path: '**', redirectTo: '/login' } // Wildcard route for a 404 page
];
