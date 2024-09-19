import { IconButton, Tooltip } from '@mui/material';
import Checkbox from '@mui/material/Checkbox';
import Paper from '@mui/material/Paper';
import { alpha } from '@mui/material/styles';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Toolbar from '@mui/material/Toolbar';
import Typography from '@mui/material/Typography';
import PropTypes from 'prop-types';
import { useEffect, useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import TablePagging from '~/components/TablePagging';
import services from '~/plugins/services';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import AddClaimDialog from './AddClaimDialog';
import { enqueueSnackbar } from 'notistack';
function EnhancedTableHead(props) {
    const { onSelectAllClick, numSelected, rowCount } =
        props;

    return (
        <TableHead>
            <TableRow>
                <TableCell padding="checkbox">
                    <Checkbox
                        color="primary"
                        indeterminate={numSelected > 0 && numSelected < rowCount}
                        checked={rowCount > 0 && numSelected === rowCount}
                        onChange={onSelectAllClick}
                        inputProps={{
                            'aria-label': 'select all desserts',
                        }}
                    />
                </TableCell>
                <TableCell>
                    <b>Type</b>
                </TableCell>
                <TableCell>
                    <b>Value</b>
                </TableCell>
            </TableRow>
        </TableHead>
    );
}

EnhancedTableHead.propTypes = {
    numSelected: PropTypes.number.isRequired,
    onSelectAllClick: PropTypes.func.isRequired,
    rowCount: PropTypes.number.isRequired
};

function EnhancedTableToolbar(props) {
    const { numSelected, setApiCall } = props;
    return (
        <Toolbar
            sx={[
                {
                    pl: { sm: 2 },
                    pr: { xs: 1, sm: 1 },
                },
                numSelected > 0 && {
                    bgcolor: (theme) =>
                        alpha(theme.palette.primary.main, theme.palette.action.activatedOpacity),
                },
            ]}
        >
            {numSelected > 0 ? (
                <Typography
                    sx={{ flex: '1 1 100%' }}
                    color="inherit"
                    variant="subtitle1"
                    component="div"
                >
                    {numSelected} selected
                </Typography>
            ) : (
                <Typography
                    sx={{ flex: '1 1 100%' }}
                    variant="h6"
                    id="tableTitle"
                    component="div"
                >
                    Claims
                </Typography>
            )}

            {numSelected > 0 && (
                <Tooltip title="Add Claims">
                    <AddClaimDialog selected={numSelected} setApiCall={setApiCall} />
                </Tooltip>
            )}
        </Toolbar>
    );
}

EnhancedTableToolbar.propTypes = {
    numSelected: PropTypes.number.isRequired,
};

function ClaimTable({ claims, setClaims, userId, pagination, setPagination,
    selected,
    setSelected,
    setTab,
    setCurrentPage,
    currentPage
}) {

    const [loading, setLoading] = useState(false);
    const [apiCall, setApiCall] = useState(null);

    useEffect(() => {
        console.log("zoday");
        setApiCall(1);
    }, [currentPage]);
    console.log(apiCall);

    useEffect(() => {
        if (apiCall !== null) {
            setLoading(true)
        }
    }, [apiCall])
    useEffect(() => {
        if (apiCall === 1) {
            handleGetClaims();
        }
        else if (apiCall === 2) {
            handleAddClaims();
        }
    }, [loading]);

    const handleGetClaims = async () => {
        try {
            await services.ClaimManagementAPI.getClaims((res) => {
                setClaims(res.result);
                res.pagination.currentSize = res.result.length
                setPagination(res.pagination);
            }, (err) => {
                console.log(err);
            }, {
                userId: userId,
                pageNumber: currentPage || 1
            })
            setLoading(false);
            setApiCall(null);
        } catch (error) {
            console.log(error);
        }
    }

    const handleAddClaims = async () => {
        try {
            await services.UserManagementAPI.assignClaims(userId, {
                userId: userId,
                userClaimIds: selected
            },
                (res) => {
                    console.log(res);
                    setSelected([]);
                    setTab(0);
                    enqueueSnackbar("Assign claim successfully!", { variant: "success" });
                }, (err) => {
                    console.log(err);
                    enqueueSnackbar("Assign claim failed!", { variant: "error" });
                }
            )
            setLoading(false);
            setApiCall(null);
        } catch (error) {
            console.log(error);
        }
    }
    const handleClick = (event, id) => {
        const selectedIndex = selected.indexOf(id);
        let newSelected = [];

        if (selectedIndex === -1) {
            newSelected = newSelected.concat(selected, id);
        } else if (selectedIndex === 0) {
            newSelected = newSelected.concat(selected.slice(1));
        } else if (selectedIndex === selected.length - 1) {
            newSelected = newSelected.concat(selected.slice(0, -1));
        } else if (selectedIndex > 0) {
            newSelected = newSelected.concat(
                selected.slice(0, selectedIndex),
                selected.slice(selectedIndex + 1),
            );
        }
        setSelected(newSelected);
    };

    const handleSelectAllClick = (event) => {
        if (event.target.checked) {
            const newSelected = claims.map((n) => n.id);
            setSelected(newSelected);
            return;
        }
        setSelected([]);
    };
    const isSelected = (id) => selected.indexOf(id) !== -1;
    return (
        <Paper sx={{ width: '100%', overflow: 'hidden' }}>
            <EnhancedTableToolbar numSelected={selected.length} setApiCall={setApiCall} />
            <TableContainer sx={{ maxHeight: 440 }}>
                <Table size='small'>
                    <EnhancedTableHead
                        numSelected={selected.length}
                        onSelectAllClick={handleSelectAllClick}
                        rowCount={claims?.length} />
                    <TableBody>
                        {claims?.map((claim, index) => {
                            const isItemSelected = isSelected(claim.id);
                            const labelId = `enhanced-table-checkbox-${index}`;

                            return (
                                <TableRow
                                    hover
                                    onClick={(event) => handleClick(event, claim.id)}
                                    role="checkbox"
                                    aria-checked={isItemSelected}
                                    tabIndex={-1}
                                    key={claim.id}
                                    selected={isItemSelected}
                                    sx={{ cursor: 'pointer' }}
                                >
                                    <TableCell padding="checkbox">
                                        <Checkbox
                                            color="primary"
                                            checked={isItemSelected}
                                            inputProps={{
                                                'aria-labelledby': labelId,
                                            }}
                                        />
                                    </TableCell>
                                    <TableCell align="left">{claim.claimType}</TableCell>
                                    <TableCell align="left">{claim.claimValue}</TableCell>
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>
                <TablePagging pagination={pagination} setPagination={setPagination} setCurrentPage={setCurrentPage} />
            </TableContainer>
            <LoadingComponent open={loading} setOpen={setLoading} />
        </Paper>
    )
}

export default ClaimTable
