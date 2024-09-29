import { Avatar, AvatarGroup, Box, Pagination, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import ListUserModal from '../RoleClaimModal/ListUserModal';
import { useEffect, useState } from 'react';
import services from '~/plugins/services';

function RoleTable({roles, setRoles}) {
    
    return (
        <TableContainer component={Paper} sx={{ mt: "20px" }}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>No</TableCell>
                        <TableCell>Role</TableCell>
                        <TableCell>Users</TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {
                        roles.map((r, index) => {
                            return (
                                <TableRow key={r.id || index}>
                                    <TableCell>{r.id}</TableCell>
                                    <TableCell>
                                        {r.name}
                                    </TableCell>
                                    <TableCell>
                                        {r.users.length === 0 ? 'Empty' : <ListUserModal totalUsersInRole={r.totalUsersInRole} users={r.users} roles={roles} setRoles={setRoles} roleId={r.id} />}
                                    </TableCell>

                                </TableRow>
                            )
                        })
                    }
                </TableBody>
            </Table>

        </TableContainer>
    )
}

export default RoleTable
