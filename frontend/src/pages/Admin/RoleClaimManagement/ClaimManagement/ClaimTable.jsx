import { Avatar, AvatarGroup, Box, Button, Pagination, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import ListUserModal from '../RoleClaimModal/ListUserModal';
import { format } from 'date-fns';
import TablePagging from '~/Components/TablePagging';
function ClaimTable({totalUser, claim, setClaim, pagination, setPagination, currentPage, setCurrentPage }) {

    return (
        <TableContainer component={Paper} sx={{ mt: "20px" }}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>ID</TableCell>
                        <TableCell>Type</TableCell>
                        <TableCell>Value</TableCell>
                        <TableCell>Users</TableCell>
                        <TableCell>CreatedDate</TableCell>
                        <TableCell>UpdatedDate</TableCell>
                        <TableCell>Actions</TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {
                        claim.map((a, index) => {
                            return (
                                <TableRow key={index}>
                                    <TableCell>{a.id}</TableCell>
                                    <TableCell>
                                        {a.claimType}
                                    </TableCell>
                                    <TableCell>
                                        {a.claimValue}
                                    </TableCell>
                                    <TableCell>
                                        <ListUserModal totalUser={a.totalUser} users={a.users} claimId={a.id} claim={claim} setClaim={setClaim}/>
                                    </TableCell>
                                    <TableCell>
                                        {format(a.createdDate, 'dd-MM-yyyy')}
                                    </TableCell>
                                    <TableCell>
                                        {format(a.updatedDate, 'dd-MM-yyyy')}
                                    </TableCell>
                                    <TableCell>
                                        <Button variant='contained' color='primary'>Rest</Button>
                                    </TableCell>
                                </TableRow>
                            )
                        })
                    }
                </TableBody>
            </Table>

            <TablePagging pagination={pagination} setPagination={setPagination} setCurrentPage={setCurrentPage} />
        </TableContainer>
    )
}

export default ClaimTable
