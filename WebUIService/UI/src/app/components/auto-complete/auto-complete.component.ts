import { Component, OnInit, Input } from '@angular/core';
import { CachedItem } from 'src/app/domain/entities/cached-item';
import { Repository } from 'src/app/domain/entities/repository';

@Component({
  selector: 'app-auto-complete',
  templateUrl: './auto-complete.component.html',
  styleUrls: ['./auto-complete.component.less']
})
export class AutoCompleteComponent implements OnInit {

  @Input() items: CachedItem<Repository>[] = null;

  constructor() { }

  ngOnInit() {
  }

}
