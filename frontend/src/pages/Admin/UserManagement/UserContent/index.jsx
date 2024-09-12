import { Box, Button, TextField, Typography } from '@mui/material'
import UserTable from './UserTable'
import { useEffect, useState } from 'react'
import services from '~/plugins/services'
import LoadingComponent from '~/components/LoadingComponent';
import UserCreation from '../UserProfileModal/UserCreation';
function UserContent() {
    const [users, setUsers] = useState(null);
    const [pagination, setPagination] = useState(null);
    const [loading, setLoading] = useState(true);
    const [searchValue, setSearchValue] = useState("");
    useEffect(() => {
        services.UserManagementAPI.getUsers((res) => {
            const updatedResult = res.result.map((r) => {
                let splitedRole = r.role.split(",");
                r.role = splitedRole;
                return r;
            })
            setUsers(updatedResult);
            res.pagination.currentSize = updatedResult.length
            setPagination(res.pagination);
            setLoading(false);
        }, (err) => {
            setLoading(false);
        }, {
            searchType: "all"
        })
    }, []);

    useEffect(() => {
        if (searchValue.trim() !== "") {
            const handler = setTimeout(() => {
                setLoading(true);
            }, 2000)
            return () => {
                clearTimeout(handler)
            }
        }
    }, [searchValue])
    const handleSearch = (e) => {
        setSearchValue(e.target.value);
    }

    useEffect(() => {
        services.UserManagementAPI.getUsers((res) => {
            const updatedResult = res.result.map((r) => {
                let splitedRole = r.role.split(",");
                r.role = splitedRole;
                return r;
            })
            console.log(updatedResult);
            setUsers(updatedResult);
            res.pagination.currentSize = updatedResult.length
            setPagination(res.pagination);
            setLoading(false);
        }, (err) => {
            setLoading(false);
        }, {
            searchValue: searchValue,
            searchType: "all"
        })
    }, [loading])
    return (
        <Box sx={{
            height: (theme) => `calc(100vh - ${theme.myapp.adminHeaderHeight})`,
            width: "100%",
            position: "relative",
            marginTop: (theme) => theme.myapp.adminHeaderHeight
        }}>
            <Box sx={{
                width: "100%", bgcolor: "white", p: "20px",
                borderRadius: "10px",
                boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px"
            }}>
                <Typography variant='h6'>Search</Typography>
                <Box sx={{
                    width: "100%",
                    display: "flex",
                    justifyContent: "end",
                    gap: 2
                }}>
                    <TextField id="outlined-basic" label="Search user" variant="outlined"
                        onChange={handleSearch}
                        sx={{
                            padding: "0",
                            width: "300px"
                        }} size='small' />
                    <UserCreation />
                </Box>
                <Box>
                    <UserTable users={users} pagination={pagination} setPagination={setPagination} setUsers={setUsers} />
                </Box>
            </Box>
            <LoadingComponent open={loading} setLoading={setLoading} />
        </Box>
    )
}

export default UserContent
