import { Repository } from '../../entities/repository';

export class Item {
    constructor(public repository: Repository, public score: number) { }
}

export class SearchOutput {
    constructor(public items: Item[]) { }
}
