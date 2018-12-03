import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SearchInput } from '../domain/messages/search/search-input';
import { SearchOutput } from '../domain/messages/search/search-output';
import { Result } from '../domain/interop/result';
import { StatusOutput } from '../domain/messages/session/status-output';
import { OAuthUrlOutput } from '../domain/messages/oauth/oauth-url-output';
import { AutoCompleteOutput } from '../domain/messages/search/auto-complete-output';

const SEARCH_ENDPOINT = '/api/Search/Perform';
const SUGGEST_ENDPOINT = '/api/Search/Suggest';
const AUTOCOMPLETE_ENDPOINT = '/api/Search/AutoComplete';
const SESSION_STATUS_ENDPOINT = '/api/Session/Status';
const OAUTH_CREATE_URL_ENDPOINT = "/api/OAuth/CreateUrl";


@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private _http: HttpClient) { }

  search(input: SearchInput) {
    return this._http.post<Result<SearchOutput>>(SEARCH_ENDPOINT, input);
  }

  suggest(input: SearchInput) {
    return this._http.post<SearchOutput>(SUGGEST_ENDPOINT, input);
  }

  autoComplete(input: SearchInput) {
    return this._http.post<AutoCompleteOutput>(AUTOCOMPLETE_ENDPOINT, input);
  }

  getSessionStatus() {
    return this._http.get<StatusOutput>(SESSION_STATUS_ENDPOINT);
  }

  createOAuthUrl() {
    return this._http.post<OAuthUrlOutput>(OAUTH_CREATE_URL_ENDPOINT, {});
  }
}
