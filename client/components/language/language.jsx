import React from 'react';
import _ from 'lodash';
import { connect } from 'react-redux';
import {Table, Column, Cell} from 'fixed-data-table';
import {Badge} from 'react-bootstrap';

function getLibrary(state){
    return {library: state.get('fixtures')};
}

function FixtureTable({library}){
    const fixtures = _.sortBy(_.values(library.fixtures), x => x.title);

    function TitleCell(props){
      const fixture = fixtures[props.rowIndex];
      const href = '#/fixture/' + fixture.key;

      return (
        <Cell {...props}><a href={href}>{fixture.title}</a></Cell>
      );
    }

    function ImplementationCell(props){
      return (
        <Cell {...props}>{fixtures[props.rowIndex].implementation}</Cell>
      );
    }

    function gotoErrors(){
      window.location = "/#/grammar-errors";
    }

    function ErrorCell(props){
      var count = fixtures[props.rowIndex].errorCount();

      if (count > 0){
        return (<Cell {...props} style={{align: 'center', verticalAlign: 'middle'}}><Badge title="Click to see the grammar errors" onClick={gotoErrors}>{count}</Badge></Cell>)
      }

      return (<Cell />);
    }

    function getRowClazz(index){
      if (fixtures[index].errorCount() > 0){
        return 'bg-warning';
      }

      return '';
    }

    return (

      <Table
        rowHeight={50}
        rowsCount={fixtures.length}
        width={1000}
        height={500}
        headerHeight={50}
        rowClassNameGetter={getRowClazz}>
        <Column
          header={<Cell>Errors</Cell>}
          cell={ErrorCell}
          width={75}
          align='center'
        />
        <Column
          header={<Cell>Fixture Title</Cell>}
          cell={<TitleCell />}
          width={200}
        />
        <Column
          header={<Cell>Implementation</Cell>}
          cell={<ImplementationCell />}
          width={500}
        />
      </Table>
    );

}


module.exports = connect(getLibrary)(FixtureTable);
