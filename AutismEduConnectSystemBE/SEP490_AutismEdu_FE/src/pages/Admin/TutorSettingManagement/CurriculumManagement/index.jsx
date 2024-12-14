import {
  Box,
  IconButton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Paper,
  TextField,
  InputAdornment,
  MenuItem,
  FormControl,
  InputLabel,
  Select,
  Pagination,
  Button
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import SearchIcon from '@mui/icons-material/Search';
import VisibilityIcon from '@mui/icons-material/Visibility';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import DescriptionModal from './Modal/DescriptionModal';
import RejectModal from './Modal/RejectModal';
import ConfirmRejectDialog from './Modal/ConfirmRejectDialog';
import ConfirmAcceptDialog from './Modal/ConfirmAcceptDialog';
import { format } from 'date-fns';
import emptyBook from '~/assets/images/icon/emptybook.gif'

const CurriculumManagement = () => {
  const [loading, setLoading] = useState(false);
  const [curriculumList, setCurriculumList] = useState([]);
  const [openDialogDes, setOpenDialogDes] = useState(false);
  const [openDialogReason, setOpenDialogReason] = useState(false);
  const [selectedCurricurlum, setSelectedCurriculum] = useState(null);
  const [selectedReason, setSelectedReason] = useState('');
  const [currentSelectedId, setCurrentSelectedId] = useState(0);
  const [openDialogAccept, setOpenDialogAccept] = useState(false);
  const [openDialogReject, setOpenDialogReject] = useState(false);

  const [filters, setFilters] = React.useState({
    search: '',
    status: 'all',
    orderBy: 'createdDate',
    sort: 'desc',
  });

  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 5,
    total: 10,
  });

  useEffect(() => {
    handleGetCurriculumList();
  }, [filters, pagination.pageNumber]);

  const handleGetCurriculumList = async () => {
    try {
      setLoading(true);
      await services.CurriculumManagementAPI.getCurriculums((res) => {
        if (res?.result) {
          setCurriculumList(res.result);
          setPagination(res.pagination);
        }
      }, (error) => {
        console.log(error);
      }, {
        ...filters,
        pageNumber: pagination.pageNumber,
        pageSize: pagination.pageSize,
      });
    } catch (error) {
      console.log(error);
    } finally {
      setLoading(false);
    }
  };

  const handleChangeStatus = async (id, statusChange, rejectionReason) => {
    try {
      setLoading(true);
      await services.CurriculumManagementAPI.changeStatusCurriculum(id, {
        id,
        statusChange,
        rejectionReason
      }, (res) => {
        if (res?.result) {
          let newData = curriculumList.find((r) => r.id === id);
          newData.requestStatus = res.result.requestStatus;
          newData.rejectionReason = res.result.rejectionReason;
          setCurriculumList(curriculumList);
          enqueueSnackbar('Đổi trạng thái thành công!', { variant: 'success' });
        }
      }, (error) => {
        enqueueSnackbar(error.error[0], { variant: 'error' });
        console.log(error);
      });
    } catch (error) {
      console.log(error);

    } finally {
      setLoading(false);
    }
  };


  const handleAccept = async (id) => {
    await handleChangeStatus(id, 1, "");
    console.log(`Accepted request ID: ${id}`);
  };

  const handleReject = async (id, reason) => {
    await handleChangeStatus(id, 0, reason);
    console.log(`Rejected request ID: ${id} with reason: ${reason}`);
  };

  const handleFilterChange = (key) => (event) => {
    setFilters({
      ...filters,
      [key]: event.target.value,
    });
    setPagination({
      pageNumber: 1,
      pageSize: 5,
      total: 10,
    });
  };

  const handlePageChange = (event, value) => {
    setPagination({ ...pagination, pageNumber: value });
  };


  const totalPages = Math.ceil(pagination.total / pagination.pageSize);

  const getStatusLabel = (status) => {

    switch (status) {
      case 1:
        return "Chấp nhận";
      case 2:
        return "Đang chờ";
      case 0:
        return "Từ chối";
      default:
        return "Không xác định";
    }
  };

  return (
    <Box sx={{
      height: (theme) => `calc(90vh - ${theme.myapp.adminHeaderHeight})`,
      p: 0,
    }}>
      <Box
        sx={{
          width: "100%",
          bgcolor: "white",
          p: "30px",
          borderRadius: "10px",
          boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
          display: "flex",
          flexDirection: "column",
          gap: 2,
          height: curriculumList?.length >= 5 ? 'auto' : '650px',
          margin: "auto"
        }}
      >
        <Typography variant="h4" sx={{ mb: 3 }}>
          Quản lý khung chương trình
        </Typography>
        <Stack direction={'row'} justifyContent={'space-between'} alignItems="center" sx={{ width: "100%", mb: 2 }} spacing={3}>
          <Box sx={{ flex: 2, mr: 3 }}>
            <TextField
              fullWidth
              size='small'
              label="Tìm kiếm theo email"
              value={filters.search}
              onChange={handleFilterChange('search')}
              sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
              InputProps={{
                endAdornment: (
                  <InputAdornment position="end">
                    <SearchIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Box>

          <Stack direction={'row'} justifyContent={'flex-end'} spacing={2} sx={{ flex: 1 }}>
            <Box sx={{ flexBasis: '45%' }}>
              <FormControl fullWidth size='small'>
                <InputLabel id="status-select-label">Trạng thái</InputLabel>
                <Select
                  labelId="status-select-label"
                  value={filters.status}
                  label="Trạng thái"
                  onChange={handleFilterChange('status')}
                  sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                >
                  <MenuItem value={'all'}>Tất cả</MenuItem>
                  <MenuItem value={'approve'}>Đã chấp nhận</MenuItem>
                  <MenuItem value={'pending'}>Đang chờ</MenuItem>
                  <MenuItem value={'reject'}>Từ chối</MenuItem>
                </Select>
              </FormControl>
            </Box>

            <Box sx={{ flexBasis: '45%' }}>
              <FormControl fullWidth size='small'>
                <InputLabel id="sort-select-label">Thứ tự</InputLabel>
                <Select
                  labelId="sort-select-label"
                  value={filters.sort}
                  label="Thứ tự"
                  onChange={handleFilterChange('sort')}
                  sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                >
                  <MenuItem value="asc">Tăng dần theo ngày</MenuItem>
                  <MenuItem value="desc">Giảm dần theo ngày</MenuItem>
                </Select>
              </FormControl>
            </Box>
          </Stack>
        </Stack>

        {curriculumList.length === 0 ? <Box sx={{ textAlign: "center" }}>
          <img src={emptyBook} style={{ height: "200px" }} />
          <Typography>Hiện chưa có dữ liệu!</Typography>
        </Box> :
          <>
            <TableContainer component={Paper} sx={{ borderRadius: 2, boxShadow: 3 }} hover>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 'bold' }}>ID</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Email</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Họ và tên</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Miêu tả</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Ngày tạo</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Trạng thái</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Lý do từ chối</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Hành động</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {curriculumList?.map((curriculum, index) => (
                    <TableRow key={curriculum.id} hover>
                      <TableCell>{index + 1 + (pagination?.pageNumber - 1) * 5}</TableCell>
                      <TableCell>{curriculum?.submitter?.email ?? curriculum?.tutorRegistrationRequest?.email}</TableCell>
                      <TableCell>{curriculum?.submitter?.fullName ?? curriculum?.tutorRegistrationRequest?.fullName}</TableCell>
                      <TableCell>
                        <IconButton disabled={!curriculum?.description} color="primary" onClick={() => {
                          setSelectedCurriculum(curriculum);
                          setOpenDialogDes(true);
                        }}>
                          <VisibilityIcon />
                        </IconButton>
                      </TableCell>
                      <TableCell>{curriculum?.createdDate && format(curriculum.createdDate, 'HH:mm dd/MM/yyyy')}</TableCell>

                      <TableCell>
                        <Button
                          variant="outlined"
                          color={
                            curriculum.requestStatus === 1 ? 'success' :
                              curriculum.requestStatus === 0 ? 'error' :
                                'warning'
                          }
                          size="small"
                          sx={{ borderRadius: 2, textTransform: 'none' }}
                        >
                          {getStatusLabel(curriculum?.requestStatus)}
                        </Button>

                      </TableCell>
                      <TableCell>
                        <IconButton disabled={!curriculum?.rejectionReason} color="primary" onClick={() => {
                          setSelectedReason(curriculum?.rejectionReason);
                          setOpenDialogReason(true);
                        }}>
                          <VisibilityIcon />
                        </IconButton>
                      </TableCell>
                      <TableCell>
                        <>
                          <IconButton
                            disabled={
                              curriculum?.requestStatus !== 2
                            }
                            color="success"
                            onClick={() => {
                              setCurrentSelectedId(curriculum?.id);
                              setOpenDialogAccept(true);
                            }}
                          >
                            <CheckCircleIcon />
                          </IconButton>
                          <IconButton
                            disabled={
                              curriculum?.requestStatus !== 2
                            }
                            color="error"
                            onClick={() => {
                              setCurrentSelectedId(curriculum?.id);
                              setOpenDialogReject(true);
                            }}
                          >
                            <CancelIcon />
                          </IconButton>
                        </>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
            <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
              <Pagination
                count={totalPages}
                page={pagination.pageNumber}
                onChange={handlePageChange}
                color="primary"
              />
            </Stack>
          </>
        }
        {openDialogDes &&
          <DescriptionModal open={openDialogDes} handleClose={() => setOpenDialogDes(false)} curriculum={selectedCurricurlum} />
        }
        {
          openDialogReason && <RejectModal open={openDialogReason} handleClose={() => setOpenDialogReason(false)} reason={selectedReason} />
        }
        {openDialogReject && <ConfirmRejectDialog open={openDialogReject} onClose={() => setOpenDialogReject(false)} onConfirm={(reason) => { handleReject(currentSelectedId, reason); setOpenDialogReject(false); }} />}
        {openDialogAccept && <ConfirmAcceptDialog open={openDialogAccept} onClose={() => setOpenDialogAccept(false)} onConfirm={() => { handleAccept(currentSelectedId); setOpenDialogAccept(false); }} />}
        <LoadingComponent open={loading} setOpen={setLoading} />
      </Box>
    </Box >
  );
};

export default CurriculumManagement;
