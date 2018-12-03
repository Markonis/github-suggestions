import { Injectable } from '@angular/core';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class OauthService {

  authUrl = '';

  constructor(private _api: ApiService) { }

  createUrl() {
    this._api.createOAuthUrl().subscribe(output => {
      this.authUrl = output.url;
    });
  }
}
