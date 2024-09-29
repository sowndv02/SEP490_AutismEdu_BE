import { Box, Button, FormControl, InputLabel, MenuItem, Select, TextField, Typography } from '@mui/material'
import ClaimTable from './ClaimTable'
import { useEffect, useState } from 'react';
import ClaimModal from '../RoleClaimModal/ClaimModal';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
import SearchIcon from '@mui/icons-material/Search';

function ClaimManagement() {
    const [loading, setLoading] = useState(false);
    const [claim, setClaim] = useState([]);
    const [pagination, setPagination] = useState(null);
    const [selected, setSelected] = useState([]);
    const [currentPage, setCurrentPage] = useState(1);
    const [searchType, setSearchType] = useState('all');
    const [searchValue, setSearchValue] = useState('');
    useEffect(() => {
        handleSearch();
    }, [currentPage]);
    const handleGetClaims = async () => {
        try {
            setLoading(true);
            await services.ClaimManagementAPI.getClaims((res) => {
                setClaim(res.result);
                console.log(res);
                res.pagination.currentSize = res.result.length
                setPagination(res.pagination);
            }, (err) => {
                console.log(err);
            }, {
                pageNumber: currentPage || 1
            });
            setLoading(false);
        } catch (error) {
            console.log(error);
            setLoading(false);
        }
    }

    const handleChange = (event) => {
        setSearchType(event.target.value);
    };
    const handleChangeSearchValue = (e) => {
        setSearchValue(e.target.value);
    }
    const handleSearch = async (e) => {
        try {
            setLoading(true);
            await services.ClaimManagementAPI.getClaims((res) => {
                setClaim(res.result);
                console.log(res);
                res.pagination.currentSize = res.result.length
                setPagination(res.pagination);
            }, (err) => {
                console.log(err);
            }, {
                searchType,
                searchValue,
                pageNumber: currentPage || 1
            });
            setLoading(false);
        } catch (error) {
            console.log(error);
            setLoading(false);
        }
    }
    return (
        <Box sx={{
            width: "100%", bgcolor: "white", p: "20px",
            borderRadius: "10px",
            boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
            position: "relative"
        }}>
            <Typography variant='h6'>Claims</Typography>
            <Box sx={{
                width: "100%",
                display: "flex",
                justifyContent: "space-between",
                gap: 2,
                marginTop: "30px"
            }}>
                <Box sx={{ display: "flex", gap: 3 }}>
                    <FormControl size='small' sx={{ width: "120px" }}>
                        <InputLabel id="type-claim">Type</InputLabel>
                        <Select
                            labelId="type-claim"
                            id="type-claim-select"
                            value={searchType}
                            label="Type"
                            onChange={handleChange}
                        >
                            <MenuItem value={'all'}>All</MenuItem>
                            <MenuItem value={'Create'}>Create</MenuItem>
                            <MenuItem value={'View'}>View</MenuItem>
                            <MenuItem value={'Update'}>Update</MenuItem>
                            <MenuItem value={'Delete'}>Delete</MenuItem>
                            <MenuItem value={'Assign'}>Assign</MenuItem>
                        </Select>
                    </FormControl>
                    <TextField size='small' id="outlined-basic" label="Search claim" variant="outlined"
                        sx={{
                            width: "300px"
                        }} onChange={handleChangeSearchValue} value={searchValue}/>
                    <Button variant='contained' color='primary' startIcon={<SearchIcon />} onClick={handleSearch}>Search</Button>
                </Box>
                <ClaimModal/>
            </Box>
            <Box>
                <ClaimTable claim={claim} setClaim={setClaim} pagination={pagination}
                    setPagination={setPagination}
                    currentPage={currentPage}
                    setCurrentPage={setCurrentPage} 
                    />
                <LoadingComponent open={loading} setOpen={setLoading} />
            </Box>
        </Box>
    )
}

export default ClaimManagement
